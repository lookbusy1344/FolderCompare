use std::fmt::{Display, Formatter};
use std::str::FromStr;
use strum::EnumString;

/// convert comparison string into an instance of `FileDataCompareOption`
pub fn parse_comparer(
    comparer_str: &Option<String>,
) -> Result<FileDataCompareOption, strum::ParseError> {
    match comparer_str {
        Some(s) if !s.is_empty() => FileDataCompareOption::from_str(s), // a non-empty string
        _ => Ok(FileDataCompareOption::Name), // otherwise, use the default
    }
}

// =================================================================================================

// A struct to hold the hash value, without the overhead of a String
#[derive(Debug, Clone, PartialEq, Eq, Hash)]
pub struct Sha2Value {
    pub hash: [u8; 32],
}

impl Sha2Value {
    /// Create a new `Sha2Value` from a u8 slice
    pub fn new(slice: &[u8]) -> Self {
        let mut hash = [0u8; 32];
        hash.copy_from_slice(slice);
        Sha2Value { hash }
    }
}

// =================================================================================================

/// Type of comparison to use
#[derive(Debug, Copy, Clone, PartialEq, Eq, EnumString)]
#[strum(ascii_case_insensitive)]
pub enum FileDataCompareOption {
    #[strum(serialize = "name")]
    Name,
    #[strum(serialize = "namesize")]
    NameSize,
    #[strum(serialize = "hash")]
    Hash,
}

/// Represents a file path
#[derive(Debug, Clone)]
pub struct FilePath(pub String);

impl Display for FilePath {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}", self.0)
    }
}
