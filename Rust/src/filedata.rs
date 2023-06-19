use std::hash::{Hash, Hasher};
use std::{marker::PhantomData, str::FromStr};
use strum::EnumString;

// FileData struct and associated trait implementations
// This struct can be used to compare files by name, name and size, or name and hash
// According to a comparison marker struct (UniqueName, UniqueNameSize, UniqueHash)
// Thoser markers must implement the UniqueTrait trait, as well as Eq, PartialEq and Hash

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
pub struct FileData<U> {
    pub filename: String,
    pub path: String,
    pub size: u64,
    pub hash: Option<String>,
    pub phantom: PhantomData<U>,
}

// marker trait for various comparison markers
pub trait UniqueTrait {}

// these marker types are used to implement Eq, PartialEq and Hash for FileData - in 3 different ways
pub struct UniqueName;
pub struct UniqueNameSize;
pub struct UniqueHash;

impl Eq for FileData<UniqueName> {}
impl Eq for FileData<UniqueNameSize> {}
impl Eq for FileData<UniqueHash> {}
impl UniqueTrait for UniqueName {}
impl UniqueTrait for UniqueNameSize {}
impl UniqueTrait for UniqueHash {}

unsafe impl<U> Send for FileData<U> where U: UniqueTrait {}
unsafe impl<U> Sync for FileData<U> where U: UniqueTrait {}

// =================================================================================================

/// Name is unique
impl PartialEq for FileData<UniqueName> {
    fn eq(&self, other: &Self) -> bool {
        self.filename == other.filename
    }
}

impl Hash for FileData<UniqueName> {
    fn hash<H: Hasher>(&self, state: &mut H) {
        self.filename.hash(state);
    }
}

// =================================================================================================

/// Name and size are unique
impl PartialEq for FileData<UniqueNameSize> {
    fn eq(&self, other: &Self) -> bool {
        self.filename == other.filename && self.size == other.size
    }
}

impl Hash for FileData<UniqueNameSize> {
    fn hash<H: Hasher>(&self, state: &mut H) {
        self.filename.hash(state);
        self.size.hash(state);
    }
}

// =================================================================================================

/// Hash & size is unique
impl PartialEq for FileData<UniqueHash> {
    fn eq(&self, other: &Self) -> bool {
        self.hash == other.hash && self.size == other.size
    }
}

impl Hash for FileData<UniqueHash> {
    fn hash<H: Hasher>(&self, state: &mut H) {
        self.hash.hash(state);
    }
}
