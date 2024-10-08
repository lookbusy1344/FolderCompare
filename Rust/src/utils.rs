use git_version::git_version;
use sha2::Digest;
use std::collections::HashMap;
use std::fs::File;
use std::io::{BufReader, Read};
use std::path::{Path, PathBuf};

use crate::filedata::{FileDataCompareOption, Sha2Hash};
use crate::{parse_comparer, FilePath};

pub const VERSION: Option<&str> = option_env!("CARGO_PKG_VERSION");
pub const GIT_VERSION: &str = git_version!(args = ["--abbrev=40", "--always", "--dirty=+"]);
const FILE_BUFFER_SIZE: usize = 4096;

pub const HELP: &str = "\
USAGE:
    folder_compare.exe -a <folder> -b <folder> [-c <comparison>] [-r] [-f]

MANDATORY PARAMETERS:
    -a, --foldera                First folder to compare
    -b, --folderb                Second folder to compare

OPTIONS:
    -c, --comparison [value]     Comparison to use.
    -r, --raw                    Raw output, for piping
    -o, --one-thread             Only use one thread, don't scan folders in parallel
    -f, --first-only             Only show files in folder A missing from folder B (default is both)
    
Comparison can be:
    Name, NameSize or Hash. Default is Name.";

/// Configuration for the program, wrapper around various options
pub struct Config {
    pub folder1: PathBuf,
    pub folder2: PathBuf,
    pub comparer: FileDataCompareOption, // how to compare files, Name, NameSize or Hash
    pub raw: bool,                       // raw output, for piping
    pub first_only: bool, // only show files in folder A missing from folder B (default is both)
    pub one_thread: bool, // only use one thread, don't scan folders in parallel
}

/// Hash a file using the given hasher as a Digest implementation
/// Returns a `Sha2Hash`, which is a wrapper around a [u8; 32]
/// # Errors
/// Will return an error if the file cannot be opened or read
pub fn hash_file<D: Digest>(filename: &str) -> anyhow::Result<Sha2Hash> {
    let file = File::open(filename)?;
    let mut reader = BufReader::new(file);
    let mut buffer = [0u8; FILE_BUFFER_SIZE];

    let mut hasher = D::new();
    loop {
        let n = reader.read(&mut buffer)?;
        if n == 0 {
            break;
        }
        hasher.update(&buffer[..n]);
    }

    let h = hasher.finalize();

    Ok(Sha2Hash::new(&h))
}

/// Hash a string slice and return a `Sha2Hash`
pub fn hash_string<D: Digest>(text: &str) -> Sha2Hash {
    let mut hasher = D::new();
    hasher.update(text);
    let h = hasher.finalize();

    Sha2Hash::new(&h)
}

/// Hash a string slice and a size and return a `Sha2Hash`
pub fn hash_string_and_size<D: Digest>(text: &str, size: u64) -> Sha2Hash {
    let mut hasher = D::new();
    hasher.update(text);
    hasher.update(size.to_le_bytes());
    let h = hasher.finalize();

    Sha2Hash::new(&h)
}

pub fn parse_args() -> anyhow::Result<Config> {
    let mut pargs = pico_args::Arguments::from_env();
    let raw = pargs.contains(["-r", "--raw"]);
    if !raw {
        println!(
            "Folder_comparer Rust, ver: {}, commit: {}",
            VERSION.unwrap_or("?"),
            GIT_VERSION
        );
        println!();
    }

    if pargs.contains(["-h", "--help"]) {
        println!("{HELP}");
        return Err(anyhow::anyhow!("Exiting early"));
    }

    let path1: String = pargs.value_from_str(["-a", "--foldera"])?;
    let path2: String = pargs.value_from_str(["-b", "--folderb"])?;
    let comparer_str: Option<String> = pargs.opt_value_from_str(["-c", "--comparison"])?;
    let comparer_res = parse_comparer(&comparer_str);

    // additional validation

    if comparer_res.is_err() {
        return Err(anyhow::anyhow!(
            "Comparison should be Name, NameSize or Hash"
        ));
    }

    // package the config options, so they can be easily passed around

    let config = Config {
        folder1: Path::new(&path1).canonicalize()?,
        folder2: Path::new(&path2).canonicalize()?,
        comparer: comparer_res.unwrap(),
        raw,
        first_only: pargs.contains(["-f", "--first-only"]),
        one_thread: pargs.contains(["-o", "--one-thread"]),
    };

    // Check for unused arguments, and error out if there are any
    let unused = pargs.finish();
    if !unused.is_empty() {
        return Err(anyhow::anyhow!("Unused arguments: {:?}", unused));
    }

    Ok(config)
}

/// Scan A and return a vector of the records not found in B
pub fn hashmap_difference<'a>(
    a: &'a HashMap<Sha2Hash, FilePath>,
    b: &'a HashMap<Sha2Hash, FilePath>,
) -> Vec<&'a FilePath> {
    let mut diff = Vec::new();
    for (k, v) in a {
        if !b.contains_key(k) {
            diff.push(v);
        }
    }
    diff
}
