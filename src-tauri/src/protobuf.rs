#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum WireType {
    Varint = 0,
    Fixed64 = 1,
    LengthDelimited = 2,
    StartGroup = 3,
    EndGroup = 4,
    Fixed32 = 5,
}

#[derive(Debug, Clone)]
pub struct ProtoField<'a> {
    pub field_number: u32,
    pub wire_type: u8,
    pub varint: u64,
    pub data: &'a [u8],
}

pub struct ProtobufReader;

impl ProtobufReader {
    pub fn parse(buffer: &[u8]) -> Vec<ProtoField<'_>> {
        let mut fields = Vec::new();
        let mut index = 0;
        let len = buffer.len();

        while index < len {
            let (tag, tag_len) = match Self::read_varint(&buffer[index..]) {
                Some(res) => res,
                None => break,
            };
            index += tag_len;

            let wire_type = (tag & 0x07) as u8;
            let field_number = (tag >> 3) as u32;

            match wire_type {
                0 => {
                    // Varint
                    let (val, val_len) = match Self::read_varint(&buffer[index..]) {
                        Some(res) => res,
                        None => break,
                    };
                    index += val_len;
                    fields.push(ProtoField {
                        field_number,
                        wire_type,
                        varint: val,
                        data: &[],
                    });
                }
                1 => {
                    // Fixed64
                    if index + 8 > len {
                        break;
                    }
                    let slice = &buffer[index..index + 8];
                    index += 8;
                    fields.push(ProtoField {
                        field_number,
                        wire_type,
                        varint: 0,
                        data: slice,
                    });
                }
                2 => {
                    // LengthDelimited
                    let (field_len, len_len) = match Self::read_varint(&buffer[index..]) {
                        Some(res) => res,
                        None => break,
                    };
                    index += len_len;
                    let fl = field_len as usize;
                    if index + fl > len {
                        break;
                    }
                    let slice = &buffer[index..index + fl];
                    index += fl;
                    fields.push(ProtoField {
                        field_number,
                        wire_type,
                        varint: 0,
                        data: slice,
                    });
                }
                5 => {
                    // Fixed32
                    if index + 4 > len {
                        break;
                    }
                    let slice = &buffer[index..index + 4];
                    index += 4;
                    fields.push(ProtoField {
                        field_number,
                        wire_type,
                        varint: 0,
                        data: slice,
                    });
                }
                _ => {
                    // Unsupported wire type or corrupted payload, break safely
                    break;
                }
            }
        }

        fields
    }

    fn read_varint(buffer: &[u8]) -> Option<(u64, usize)> {
        let mut result = 0u64;
        let mut shift = 0;
        let mut count = 0;

        for &b in buffer {
            count += 1;
            result |= ((b & 0x7F) as u64) << shift;
            if (b & 0x80) == 0 {
                return Some((result, count));
            }
            shift += 7;
            if shift >= 64 {
                return None;
            }
        }

        None
    }
}
