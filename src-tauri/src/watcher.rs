use crate::data_service::TokenDataService;
use std::collections::HashMap;
use std::fs;
use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use std::thread;
use std::time::{Duration, SystemTime};
use tauri::{AppHandle, Emitter};

pub struct LiveSyncWatcher {
    is_running: Arc<AtomicBool>,
    data_service: Arc<TokenDataService>,
}

impl LiveSyncWatcher {
    pub fn new(data_service: Arc<TokenDataService>) -> Self {
        Self {
            is_running: Arc::new(AtomicBool::new(true)),
            data_service,
        }
    }

    pub fn start(&self, app_handle: AppHandle) {
        let is_running = self.is_running.clone();
        let data_service = self.data_service.clone();

        thread::spawn(move || {
            let mut last_mtimes: HashMap<PathBuf, SystemTime> = HashMap::new();

            while is_running.load(Ordering::Relaxed) {
                let mut changed = false;

                for dir in &data_service.monitored_directories {
                    if !dir.exists() {
                        continue;
                    }

                    if let Ok(entries) = fs::read_dir(dir) {
                        for entry in entries.flatten() {
                            let path = entry.path();
                            if let Some(ext) = path.extension().and_then(|e| e.to_str()) {
                                if ext.starts_with("db") {
                                    if let Ok(meta) = fs::metadata(&path) {
                                        if let Ok(mtime) = meta.modified() {
                                            if let Some(&old) = last_mtimes.get(&path) {
                                                if old != mtime {
                                                    last_mtimes.insert(path.clone(), mtime);
                                                    changed = true;
                                                }
                                            } else {
                                                last_mtimes.insert(path.clone(), mtime);
                                                changed = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if changed {
                    data_service.refresh_all(false);
                    let _ = app_handle.emit("token_data_updated", ());
                }

                thread::sleep(Duration::from_secs(1));
            }
        });
    }

    pub fn set_enabled(&self, enabled: bool) {
        self.is_running.store(enabled, Ordering::Relaxed);
    }

    pub fn is_enabled(&self) -> bool {
        self.is_running.load(Ordering::Relaxed)
    }
}
