use std::hash::{Hash, Hasher};
use std::{marker::PhantomData, str::FromStr};
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

    // pub fn to_u64(&self) -> u64 {
    //     let (int_bytes, _) = self.hash.split_at(8);
    //     u64::from_be_bytes(int_bytes.try_into().unwrap())
    // }

    /// for hashing
    pub fn to_usize(&self) -> usize {
        let (int_bytes, _) = self.hash.split_at(std::mem::size_of::<usize>());
        #[cfg(target_pointer_width = "64")]
        {
            u64::from_be_bytes(int_bytes.try_into().unwrap()) as usize
        }
        #[cfg(target_pointer_width = "32")]
        {
            u32::from_be_bytes(int_bytes.try_into().unwrap()) as usize
        }
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

pub struct FileData2 {
    pub filename: String,
    pub path: String,
    pub size: u64,
    pub hash: Sha2Value,
}

/// Represents a file, with name, pathname, size and optional hash. U is the type of comparison
pub struct FileData<U> {
    pub filename: String,
    pub path: String,
    pub size: u64,
    pub hash: Sha2Value,
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
