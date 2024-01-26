use std::str::FromStr;
use strum::EnumString;

// FileData struct and associated trait implementations
// This struct can be used to compare files by name, name and size, or name and hash
// According to a comparison marker struct (UniqueName, UniqueNameSize, UniqueHash)
// Those markers must implement the UniqueTrait trait, as well as Eq, PartialEq and Hash

/// convert comparison string into an instance of `FileDataCompareOption`
pub fn parse_comparer(
    compstr: &Option<String>,
) -> Result<FileDataCompareOption, strum::ParseError> {
    if compstr.is_none() || compstr.as_ref().unwrap().is_empty() {
        // use the default
        return Ok(FileDataCompareOption::Name);
    }

    FileDataCompareOption::from_str(compstr.as_ref().unwrap())
}

// =================================================================================================

// A struct to hold the hash value, without the overhead of a String
#[derive(Debug, Clone, PartialEq, Eq, Hash, Default)]
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

// Implementations for FileData and the various comparison options

/// Represents a file, with name, pathname, size and optional hash. U is the type of comparison
#[derive(Debug, Clone, PartialEq, Eq, Hash)]
pub struct FileData {
    pub filename: String,
    pub path: String,
    pub size: u64,
    pub hash: Sha2Value,
}
