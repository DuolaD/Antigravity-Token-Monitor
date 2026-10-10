import { ref, computed } from "vue";

export type SupportedLocale = "zh-CN" | "zh-TW" | "en";
export type LanguageMode = "system" | "zh-CN" | "zh-TW" | "en";

const STORAGE_KEY = "antigravity_tokens_language_mode";

export const messages = {
  "zh-CN": {
    app: {
      title: "Antigravity Tokens",
      subtitle: "Token 消耗监控",
      nav: {
        dashboard: "总览看板",
        logs: "日志明细",
        analytics: "统计分析",
        pricing: "模型费率",
        settings: "系统设置",
      },
      theme: {
        system: "自动跟随系统",
        light: "浅色模式",
        dark: "深色模式",
        lightShort: "浅色",
        darkShort: "深色",
        tooltip: "点击切换外观主题：自动跟随系统 / 浅色 / 深色",
      },
      language: {
        system: "跟随系统",
        zhCN: "简体中文",
        zhTW: "繁體中文",
        en: "English",
        tooltip: "点击切换显示语言",
      },
      liveStatus: {
        active: "实时探测监听中",
        desc: "毫秒级捕获 SQLite WAL 变更",
      },
    },
    dashboard: {
      title: "总览看板",
      subtitle: "实时监控 Google Antigravity 本地模型 Token 消耗与成本",
      selectCurrency: "选择结算货币",
      refreshNow: "立即刷新",
      cards: {
        totalCost: "累计预估花费",
        todayCost: "今日消耗",
        totalTokens: "累计总 Tokens",
        inputPrompt: "输入 Prompt:",
        thinking: "思考 Thinking:",
        totalCalls: "累计请求轮次",
        todayCalls: "今日请求",
        roundsUnit: "次",
        avgLatency: "平均响应耗时",
        perTurnLatency: "单轮交互耗时",
        avgSecResponse: "秒级平均响应",
      },
      trend: {
        title: "Token 消耗趋势折线图",
        presets: {
          week: "周",
          month: "月",
          quarter: "季度",
          halfYear: "半年",
          year: "1年",
          all: "全部",
        },
      },
      rankings: {
        topModels: "模型消耗排行榜",
        modelsCount: "共 {n} 个模型",
        topWorkspaces: "项目/工作区消耗排行",
        workspacesCount: "共 {n} 个项目",
        callsCount: "({n} 次)",
      },
    },
    logs: {
      title: "日志明细",
      subtitle: "查看每一次 LLM 调用的精确输入、输出、思考 Token 与耗时记录",
      exportCsv: "导出 CSV 报表",
      exporting: "正在导出...",
      searchPlaceholder: "搜索标题 / 模型 / UUID...",
      filters: {
        allTime: "全部时间",
        today: "今天",
        last7Days: "近 7 天",
        last30Days: "近 30 天",
        allModels: "全部模型",
        allWorkspaces: "全部工作区",
        filterBtn: "筛选",
        resetBtnTooltip: "重置筛选条件",
      },
      table: {
        time: "时间",
        dialogWorkspace: "对话 / 项目工作区",
        model: "模型",
        promptTokens: "提示 Tokens",
        completionTokens: "补全 Tokens",
        thinkingTokens: "思考 Tokens",
        totalTokens: "总计 Tokens",
        latency: "耗时",
        cost: "费用",
        details: "详情",
        empty: "暂无符合条件的调用日志记录",
        viewDetailsTooltip: "查看完整调用参数",
      },
      pagination: {
        pageInfo: "第 {page} 页 / 共 {pages} 页 (共 {total} 条记录)",
        prev: "上一页",
        next: "下一页",
      },
      modal: {
        title: "单次调用元数据详情",
        callId: "调用标识 (ID):",
        convUuid: "会话 UUID:",
        callTime: "调用时间:",
        modelName: "模型名称:",
        workspace: "所属工作区:",
        promptTokens: "Prompt Tokens:",
        candidateTokens: "Candidate Tokens:",
        thinkingTokens: "Thinking Tokens:",
        latency: "耗时 (Latency):",
        cost: "预估结算费用:",
        close: "关闭",
      },
    },
    analytics: {
      title: "统计分析",
      subtitle: "深度分析每日 Token 走势、思考深度 (Thinking Token) 与调用频次",
      refresh: "刷新分析",
      cards: {
        activeDays: "累计活跃统计跨度",
        activeDaysUnit: "个活跃日",
        activeDaysDesc: "按本地 SQLite 对话时间跨度归集",
        avgDailyTokens: "日均 Token 吞吐量",
        avgDailyDesc: "平均每个活跃日消耗的 Tokens",
        thinkingRatio: "全局显式思考 Token 占比",
        thinkingRatioDesc: "Thinking Tokens 占模型总输出的百分比",
      },
      table: {
        title: "按自然日归集明细历史",
        daysCount: "共 {n} 天记录",
        date: "日期",
        inputTokens: "输入 Tokens",
        completionTokens: "补全 Tokens",
        thinkingTokens: "思考 Tokens",
        thinkingRatio: "思考占比",
        dailyTotal: "每日总计",
        calls: "调用次数",
        callsUnit: "次",
        cost: "折算费用",
        empty: "暂无每日历史统计数据",
      },
    },
    pricing: {
      title: "模型费率配置",
      subtitle: "参考 NewAPI 模型倍率管理，自定义配置各模型每 100 万 (1M) Tokens 的结算价格",
      resetDefaults: "重置为默认",
      savePricing: "保存费率配置",
      saving: "正在保存...",
      savedSuccess: "费率与汇率配置已成功保存！",
      resetSuccess: "费率配置已重置为系统默认值！",
      currencyCard: {
        title: "货币结算与多币种汇率管理",
        syncRates: "同步最新网络汇率",
        syncingRates: "正在同步汇率...",
        targetCurrency: "结算显示币种 (Target Currency)",
        exchangeRateLabel: "1 USD 兑 {cur} 换算汇率",
        baseCurrency: "基准货币 (1.0000)",
        manualOverride: "支持手动覆盖调整",
        apiInfo: "汇率接口：Pages.dev 备用镜像 (高可用) / jsDelivr CDN (自动回退)",
        baseDate: "基准日期：",
        syncSuccess: "最新汇率同步成功！(基准日期: {date}, 来源: {source})",
        syncFail: "汇率拉取失败，已使用离线默认基准汇率",
        sourcePrimary: "Pages.dev 备用镜像",
        sourceFallback: "jsDelivr CDN",
      },
      table: {
        title: "模型费率清单",
        rulesCount: "共 {n} 个模型规则",
        modelId: "模型标识",
        displayName: "显示名称",
        promptPrice: "输入单价 ($ / 1M Tokens)",
        outputPrice: "输出单价 ($ / 1M Tokens)",
      },
    },
    settings: {
      title: "系统设置",
      subtitle: "配置外观主题、多语言、数据探测源、高频实时同步与应用偏好",
      appearance: {
        title: "外观主题",
        desc: "选择应用视觉主题模式。设置为跟随系统时，将自动响应操作系统深色/浅色外观切换。",
        light: "浅色",
        dark: "深色",
        system: "自动 (跟随系统)",
        currentTheme: "当前生效外观：",
        syncedWithOS: "（已与操作系统同步）",
      },
      language: {
        title: "语言设置 / Language",
        desc: "选择应用显示语言。默认跟随操作系统偏好，支持简体中文、繁体中文和英语自由切换。",
        system: "自动 (跟随系统)",
        zhCN: "简体中文",
        zhTW: "繁體中文",
        en: "English",
        currentLang: "当前生效语言：",
        syncedWithOS: "（已与操作系统同步）",
      },
      liveSync: {
        title: "实时监听与心跳刷新",
        label: "启用 1 秒级动态心跳探测",
        desc: "开启后应用将在后台双轨检测 SQLite WAL 写入，对话产生时毫秒级跳动更新看板与统计",
      },
      dataSource: {
        title: "已探测并绑定的 Antigravity 数据源",
        desc: "系统根据操作系统环境自动识别并挂载以下会话存储路径中的 .db 数据库文件：",
        empty: "暂未发现默认 Antigravity 会话目录，请确认 Antigravity 已在本地生成对话记录。",
      },
      about: {
        title: "关于 Antigravity Token Monitor",
        desc: "专为 Google Antigravity 设计的原生 Token 计量与费用监控客户端",
        platform: "运行平台: {os} | 架构核心: Tauri v2 + Rust 高性能底层",
        version: "版本: v{version} (轻量版 · 内存占用超低 · 跨平台通用)",
      },
    },
    chart: {
      empty: "暂无该时间段内的 Token 消耗记录",
      calls: "{calls} 次调用",
      totalTokens: "总计 Tokens:",
      inputPrompt: "输入 Prompt:",
      output: "输出 Output:",
      thinking: "思考 Thinking:",
      estimatedCost: "折算费用:",
    },
    common: {
      defaultWorkspace: "默认工作区",
      defaultTitlePrefix: "对话",
    },
  },

  "zh-TW": {
    app: {
      title: "Antigravity Tokens",
      subtitle: "Token 消耗監控",
      nav: {
        dashboard: "總覽看板",
        logs: "日誌明細",
        analytics: "統計分析",
        pricing: "模型費率",
        settings: "系統設定",
      },
      theme: {
        system: "自動跟隨系統",
        light: "淺色模式",
        dark: "深色模式",
        lightShort: "淺色",
        darkShort: "深色",
        tooltip: "點擊切換外觀主題：自動跟隨系統 / 淺色 / 深色",
      },
      language: {
        system: "跟隨系統",
        zhCN: "簡體中文",
        zhTW: "繁體中文",
        en: "English",
        tooltip: "點擊切換顯示語言",
      },
      liveStatus: {
        active: "即時探測監聽中",
        desc: "毫秒級捕獲 SQLite WAL 變更",
      },
    },
    dashboard: {
      title: "總覽看板",
      subtitle: "即時監控 Google Antigravity 本地模型 Token 消耗與成本",
      selectCurrency: "選擇結算貨幣",
      refreshNow: "立即刷新",
      cards: {
        totalCost: "累計預估花費",
        todayCost: "今日消耗",
        totalTokens: "累計總 Tokens",
        inputPrompt: "輸入 Prompt:",
        thinking: "思考 Thinking:",
        totalCalls: "累計請求輪次",
        todayCalls: "今日請求",
        roundsUnit: "次",
        avgLatency: "平均響應耗時",
        perTurnLatency: "單輪交互耗時",
        avgSecResponse: "秒級平均響應",
      },
      trend: {
        title: "Token 消耗趨勢折線圖",
        presets: {
          week: "週",
          month: "月",
          quarter: "季度",
          halfYear: "半年",
          year: "1年",
          all: "全部",
        },
      },
      rankings: {
        topModels: "模型消耗排行榜",
        modelsCount: "共 {n} 個模型",
        topWorkspaces: "專案/工作區消耗排行",
        workspacesCount: "共 {n} 個專案",
        callsCount: "({n} 次)",
      },
    },
    logs: {
      title: "日誌明細",
      subtitle: "查看每一次 LLM 調用的精確輸入、輸出、思考 Token 與耗時記錄",
      exportCsv: "導出 CSV 報表",
      exporting: "正在導出...",
      searchPlaceholder: "搜尋標題 / 模型 / UUID...",
      filters: {
        allTime: "全部時間",
        today: "今天",
        last7Days: "近 7 天",
        last30Days: "近 30 天",
        allModels: "全部模型",
        allWorkspaces: "全部工作區",
        filterBtn: "篩選",
        resetBtnTooltip: "重置篩選條件",
      },
      table: {
        time: "時間",
        dialogWorkspace: "對話 / 專案工作區",
        model: "模型",
        promptTokens: "提示 Tokens",
        completionTokens: "補全 Tokens",
        thinkingTokens: "思考 Tokens",
        totalTokens: "總計 Tokens",
        latency: "耗時",
        cost: "費用",
        details: "詳情",
        empty: "暫無符合條件的調用日誌記錄",
        viewDetailsTooltip: "查看完整調用參數",
      },
      pagination: {
        pageInfo: "第 {page} 頁 / 共 {pages} 頁 (共 {total} 條記錄)",
        prev: "上一頁",
        next: "下一頁",
      },
      modal: {
        title: "單次調用元數據詳情",
        callId: "調用標識 (ID):",
        convUuid: "會話 UUID:",
        callTime: "調用時間:",
        modelName: "模型名稱:",
        workspace: "所屬工作區:",
        promptTokens: "Prompt Tokens:",
        candidateTokens: "Candidate Tokens:",
        thinkingTokens: "Thinking Tokens:",
        latency: "耗時 (Latency):",
        cost: "預估結算費用:",
        close: "關閉",
      },
    },
    analytics: {
      title: "統計分析",
      subtitle: "深度分析每日 Token 走勢、思考深度 (Thinking Token) 與調用頻次",
      refresh: "刷新分析",
      cards: {
        activeDays: "累計活躍統計跨度",
        activeDaysUnit: "個活躍日",
        activeDaysDesc: "按本地 SQLite 對話時間跨度歸集",
        avgDailyTokens: "日均 Token 吞吐量",
        avgDailyDesc: "平均每個活躍日消耗的 Tokens",
        thinkingRatio: "全局顯式思考 Token 占比",
        thinkingRatioDesc: "Thinking Tokens 占模型總輸出的百分比",
      },
      table: {
        title: "按自然日歸集明細歷史",
        daysCount: "共 {n} 天記錄",
        date: "日期",
        inputTokens: "輸入 Tokens",
        completionTokens: "補全 Tokens",
        thinkingTokens: "思考 Tokens",
        thinkingRatio: "思考占比",
        dailyTotal: "每日總計",
        calls: "調用次數",
        callsUnit: "次",
        cost: "折算費用",
        empty: "暫無每日歷史統計數據",
      },
    },
    pricing: {
      title: "模型費率配置",
      subtitle: "參考 NewAPI 模型倍率管理，自定義配置各模型每 100 萬 (1M) Tokens 的結算價格",
      resetDefaults: "重置為預設",
      savePricing: "保存費率配置",
      saving: "正在保存...",
      savedSuccess: "費率與匯率配置已成功保存！",
      resetSuccess: "費率配置已重置為系統預設值！",
      currencyCard: {
        title: "貨幣結算與多幣種匯率管理",
        syncRates: "同步最新網路匯率",
        syncingRates: "正在同步匯率...",
        targetCurrency: "結算顯示幣種 (Target Currency)",
        exchangeRateLabel: "1 USD 兌 {cur} 換算匯率",
        baseCurrency: "基準貨幣 (1.0000)",
        manualOverride: "支援手動覆蓋調整",
        apiInfo: "匯率介面：Pages.dev 備用鏡像 (高可用) / jsDelivr CDN (自動回退)",
        baseDate: "基準日期：",
        syncSuccess: "最新匯率同步成功！(基準日期: {date}, 來源: {source})",
        syncFail: "匯率拉取失敗，已使用離線預設基準匯率",
        sourcePrimary: "Pages.dev 備用鏡像",
        sourceFallback: "jsDelivr CDN",
      },
      table: {
        title: "模型費率清單",
        rulesCount: "共 {n} 個模型規則",
        modelId: "模型標識",
        displayName: "顯示名稱",
        promptPrice: "輸入單價 ($ / 1M Tokens)",
        outputPrice: "輸出單價 ($ / 1M Tokens)",
      },
    },
    settings: {
      title: "系統設定",
      subtitle: "配置外觀主題、多語言、數據探測源、高頻即時同步與應用偏好",
      appearance: {
        title: "外觀主題",
        desc: "選擇應用視覺主題模式。設定為跟隨系統時，將自動響應作業系統深色/淺色外觀切換。",
        light: "淺色",
        dark: "深色",
        system: "自動 (跟隨系統)",
        currentTheme: "當前生效外觀：",
        syncedWithOS: "（已與作業系統同步）",
      },
      language: {
        title: "語言設定 / Language",
        desc: "選擇應用顯示語言。預設跟隨作業系統偏好，支援簡體中文、繁體中文和英語自由切換。",
        system: "自動 (跟隨系統)",
        zhCN: "簡體中文",
        zhTW: "繁體中文",
        en: "English",
        currentLang: "當前生效語言：",
        syncedWithOS: "（已與作業系統同步）",
      },
      liveSync: {
        title: "即時監聽與心跳刷新",
        label: "啟用 1 秒級動態心跳探測",
        desc: "開啟後應用將在後台雙軌檢測 SQLite WAL 寫入，對話產生時毫秒級跳動更新看板與統計",
      },
      dataSource: {
        title: "已探測並綁定的 Antigravity 數據源",
        desc: "系統根據作業系統環境自動識別並掛載以下會話存儲路徑中的 .db 資料庫檔案：",
        empty: "暫未發現預設 Antigravity 會話目錄，請確認 Antigravity 已在本地生成對話記錄。",
      },
      about: {
        title: "關於 Antigravity Token Monitor",
        desc: "專為 Google Antigravity 設計的原生 Token 計量與費用監控用戶端",
        platform: "運行平台: {os} | 架構核心: Tauri v2 + Rust 高效能底層",
        version: "版本: v{version} (輕量版 · 記憶體佔用超低 · 跨平台通用)",
      },
    },
    chart: {
      empty: "暫無該時間段內的 Token 消耗記錄",
      calls: "{calls} 次調用",
      totalTokens: "總計 Tokens:",
      inputPrompt: "輸入 Prompt:",
      output: "輸出 Output:",
      thinking: "思考 Thinking:",
      estimatedCost: "折算費用:",
    },
    common: {
      defaultWorkspace: "預設工作區",
      defaultTitlePrefix: "對話",
    },
  },

  en: {
    app: {
      title: "Antigravity Tokens",
      subtitle: "Token Usage Monitor",
      nav: {
        dashboard: "Dashboard",
        logs: "Call Logs",
        analytics: "Analytics",
        pricing: "Model Pricing",
        settings: "Settings",
      },
      theme: {
        system: "Follow System",
        light: "Light Mode",
        dark: "Dark Mode",
        lightShort: "Light",
        darkShort: "Dark",
        tooltip: "Toggle theme: Follow System / Light / Dark",
      },
      language: {
        system: "Follow System",
        zhCN: "简体中文",
        zhTW: "繁體中文",
        en: "English",
        tooltip: "Click to toggle display language",
      },
      liveStatus: {
        active: "Live Monitoring Active",
        desc: "Real-time SQLite WAL change detection",
      },
    },
    dashboard: {
      title: "Dashboard Overview",
      subtitle: "Real-time monitor for local Google Antigravity token usage & costs",
      selectCurrency: "Select Currency",
      refreshNow: "Refresh Now",
      cards: {
        totalCost: "Total Estimated Cost",
        todayCost: "Today's Cost",
        totalTokens: "Total Tokens",
        inputPrompt: "Input Prompt:",
        thinking: "Thinking:",
        totalCalls: "Total Call Rounds",
        todayCalls: "Today's Calls",
        roundsUnit: "rounds",
        avgLatency: "Avg Response Latency",
        perTurnLatency: "Per-turn Interaction",
        avgSecResponse: "Sec-level avg latency",
      },
      trend: {
        title: "Token Consumption Trend",
        presets: {
          week: "7D",
          month: "30D",
          quarter: "Quarter",
          halfYear: "Half Year",
          year: "1 Year",
          all: "All",
        },
      },
      rankings: {
        topModels: "Top Models Usage",
        modelsCount: "{n} models total",
        topWorkspaces: "Top Workspaces Usage",
        workspacesCount: "{n} workspaces total",
        callsCount: "({n} calls)",
      },
    },
    logs: {
      title: "Call Logs",
      subtitle: "Inspect prompt, output, thinking tokens and latency for every LLM call",
      exportCsv: "Export CSV",
      exporting: "Exporting...",
      searchPlaceholder: "Search title / model / UUID...",
      filters: {
        allTime: "All Time",
        today: "Today",
        last7Days: "Last 7 Days",
        last30Days: "Last 30 Days",
        allModels: "All Models",
        allWorkspaces: "All Workspaces",
        filterBtn: "Filter",
        resetBtnTooltip: "Reset filters",
      },
      table: {
        time: "Time",
        dialogWorkspace: "Conversation / Workspace",
        model: "Model",
        promptTokens: "Prompt Tokens",
        completionTokens: "Completion Tokens",
        thinkingTokens: "Thinking Tokens",
        totalTokens: "Total Tokens",
        latency: "Latency",
        cost: "Cost",
        details: "Details",
        empty: "No matching call logs found",
        viewDetailsTooltip: "View full call parameters",
      },
      pagination: {
        pageInfo: "Page {page} of {pages} ({total} records total)",
        prev: "Previous",
        next: "Next",
      },
      modal: {
        title: "Single Call Metadata Details",
        callId: "Call ID:",
        convUuid: "Session UUID:",
        callTime: "Timestamp:",
        modelName: "Model Name:",
        workspace: "Workspace:",
        promptTokens: "Prompt Tokens:",
        candidateTokens: "Candidate Tokens:",
        thinkingTokens: "Thinking Tokens:",
        latency: "Latency:",
        cost: "Estimated Cost:",
        close: "Close",
      },
    },
    analytics: {
      title: "Usage Analytics",
      subtitle: "Deep analysis of daily token trends, thinking depth and call frequency",
      refresh: "Refresh Analytics",
      cards: {
        activeDays: "Active Days Span",
        activeDaysUnit: "active days",
        activeDaysDesc: "Aggregated across local SQLite conversations",
        avgDailyTokens: "Avg Daily Tokens",
        avgDailyDesc: "Average tokens consumed per active day",
        thinkingRatio: "Overall Thinking Ratio",
        thinkingRatioDesc: "Percentage of output tokens spent on thinking",
      },
      table: {
        title: "Daily Aggregation History",
        daysCount: "{n} days recorded",
        date: "Date",
        inputTokens: "Input Tokens",
        completionTokens: "Completion Tokens",
        thinkingTokens: "Thinking Tokens",
        thinkingRatio: "Thinking %",
        dailyTotal: "Daily Total",
        calls: "Calls",
        callsUnit: "calls",
        cost: "Cost",
        empty: "No daily statistics available",
      },
    },
    pricing: {
      title: "Model Pricing Configuration",
      subtitle: "Configure custom pricing per 1 million (1M) tokens for each model",
      resetDefaults: "Reset to Defaults",
      savePricing: "Save Pricing",
      saving: "Saving...",
      savedSuccess: "Pricing and exchange rates saved successfully!",
      resetSuccess: "Pricing config reset to system defaults!",
      currencyCard: {
        title: "Currency & Multi-Currency Rates",
        syncRates: "Sync Latest Rates",
        syncingRates: "Syncing rates...",
        targetCurrency: "Target Display Currency",
        exchangeRateLabel: "1 USD to {cur} Exchange Rate",
        baseCurrency: "Base Currency (1.0000)",
        manualOverride: "Manual override supported",
        apiInfo: "Rate API: Pages.dev mirror (high availability) / jsDelivr CDN (fallback)",
        baseDate: "Base Date: ",
        syncSuccess: "Rates synced successfully! (Base date: {date}, source: {source})",
        syncFail: "Failed to fetch rates, using offline defaults",
        sourcePrimary: "Pages.dev Mirror",
        sourceFallback: "jsDelivr CDN",
      },
      table: {
        title: "Model Rate List",
        rulesCount: "{n} model rules total",
        modelId: "Model ID",
        displayName: "Display Name",
        promptPrice: "Prompt Price ($ / 1M)",
        outputPrice: "Output Price ($ / 1M)",
      },
    },
    settings: {
      title: "System Settings",
      subtitle: "Configure appearance, language, data sources, live sync and preferences",
      appearance: {
        title: "Appearance Theme",
        desc: "Choose visual appearance mode. Follow System will adapt to OS light/dark mode automatically.",
        light: "Light",
        dark: "Dark",
        system: "Auto (Follow System)",
        currentTheme: "Active Theme: ",
        syncedWithOS: " (Synced with OS)",
      },
      language: {
        title: "Language Settings",
        desc: "Select application display language. Defaults to operating system language, with Simplified Chinese, Traditional Chinese, and English options.",
        system: "Auto (Follow System)",
        zhCN: "简体中文",
        zhTW: "繁體中文",
        en: "English",
        currentLang: "Active Language: ",
        syncedWithOS: " (Synced with OS)",
      },
      liveSync: {
        title: "Live Sync & Heartbeat",
        label: "Enable 1-second Dynamic Heartbeat",
        desc: "Background dual-track monitoring detects SQLite WAL writes and updates dashboard in real time",
      },
      dataSource: {
        title: "Detected Antigravity Data Sources",
        desc: "Automatically discovered and mounted .db databases in the following session directories:",
        empty: "No default Antigravity conversation directory found yet. Please make sure Antigravity has created conversations.",
      },
      about: {
        title: "About Antigravity Token Monitor",
        desc: "Native token usage and cost monitoring client designed for Google Antigravity",
        platform: "Platform: {os} | Engine: Tauri v2 + Rust High-performance Engine",
        version: "Version: v{version} (Ultra-lightweight · Low memory · Cross-platform)",
      },
    },
    chart: {
      empty: "No token consumption records in this time range",
      calls: "{calls} calls",
      totalTokens: "Total Tokens:",
      inputPrompt: "Input Prompt:",
      output: "Output:",
      thinking: "Thinking:",
      estimatedCost: "Estimated Cost:",
    },
    common: {
      defaultWorkspace: "Default Workspace",
      defaultTitlePrefix: "Chat",
    },
  },
};

/**
 * Detect OS system language accurately.
 */
export function detectSystemLanguage(): SupportedLocale {
  const languages = typeof navigator !== "undefined"
    ? navigator.languages || [navigator.language]
    : [];

  for (const lang of languages) {
    if (!lang) continue;
    const lower = lang.toLowerCase();

    // Traditional Chinese checks (TW, HK, MO, Hant)
    if (
      lower.startsWith("zh-tw") ||
      lower.startsWith("zh-hk") ||
      lower.startsWith("zh-mo") ||
      lower.includes("hant")
    ) {
      return "zh-TW";
    }

    // Simplified Chinese checks (CN, SG, Hans, generic zh)
    if (
      lower.startsWith("zh-cn") ||
      lower.startsWith("zh-sg") ||
      lower.includes("hans") ||
      lower === "zh"
    ) {
      return "zh-CN";
    }

    // English
    if (lower.startsWith("en")) {
      return "en";
    }
  }

  return "en";
}

const storedMode = (typeof localStorage !== "undefined"
  ? (localStorage.getItem(STORAGE_KEY) as LanguageMode)
  : null) || "system";

export const languageMode = ref<LanguageMode>(
  storedMode === "zh-CN" ||
  storedMode === "zh-TW" ||
  storedMode === "en" ||
  storedMode === "system"
    ? storedMode
    : "system"
);

export const systemLocale = ref<SupportedLocale>(detectSystemLanguage());

if (typeof window !== "undefined") {
  window.addEventListener("languagechange", () => {
    systemLocale.value = detectSystemLanguage();
  });
}

export const currentLocale = computed<SupportedLocale>(() => {
  if (languageMode.value === "system") {
    return systemLocale.value;
  }
  return languageMode.value;
});

export function setLanguageMode(mode: LanguageMode) {
  languageMode.value = mode;
  if (typeof localStorage !== "undefined") {
    localStorage.setItem(STORAGE_KEY, mode);
  }
}

/**
 * Type-safe translation getter.
 * Supports dot paths like 'dashboard.cards.totalCost' and parameter interpolation {n}.
 */
export function t(path: string, params?: Record<string, string | number>): string {
  const locale = currentLocale.value;
  const dict = messages[locale] || messages["zh-CN"];

  const keys = path.split(".");
  let cur: any = dict;

  for (const k of keys) {
    if (cur && typeof cur === "object" && k in cur) {
      cur = cur[k];
    } else {
      // Fallback to zh-CN if key missing
      let fb: any = messages["zh-CN"];
      for (const fbk of keys) {
        if (fb && typeof fb === "object" && fbk in fb) {
          fb = fb[fbk];
        } else {
          return path;
        }
      }
      cur = fb;
      break;
    }
  }

  if (typeof cur !== "string") {
    return path;
  }

  if (!params) {
    return cur;
  }

  let result = cur;
  for (const [pk, pv] of Object.entries(params)) {
    result = result.split(`{${pk}}`).join(String(pv));
  }
  return result;
}
