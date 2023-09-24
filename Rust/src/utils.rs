use git_version::git_version;
use sha2::Digest;
use std::fs::File;
use std::io::{BufReader, Read};
use std::path::PathBuf;

use crate::filedata::{FileDataCompareOption, Sha2Value};

pub const VERSION: Option<&str> = option_env!("CARGO_PKG_VERSION");
pub const GIT_VERSION: &str = git_version!(args = ["--abbrev=40", "--always", "--dirty=+"]);

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

const BUFFER_SIZE: usize = 4096;

/// Configuration for the program, wrapper around various options
pub struct Config {
    pub folder1: PathBuf,
    pub folder2: PathBuf,
    pub comparer: FileDataCompareOption, // how to compare files, Name, NameSize or Hash
    pub raw: bool,                       // raw output, for piping
    pub firstonly: bool, // only show files in folder A missing from folder B (default is both)
    pub onethread: bool, // only use one thread, don't scan folders in parallel
}

/// Hash a file using the given hasher as a Digest implementation
/// Returns a `Sha2Value`, which is a wrapper around a [u8; 32]
/// # Errors
/// Will return an error if the file cannot be opened or read
pub fn hash_file<D: Digest>(filename: &str) -> anyhow::Result<Sha2Value> {
    let file = File::open(filename)?;
    let mut reader = BufReader::new(file);
    let mut buffer = [0u8; BUFFER_SIZE];

    let mut hasher = D::new();
    loop {
        let n = reader.read(&mut buffer)?;
        if n == 0 {
            break;
        }
        hasher.update(&buffer[..n]);
    }

    let h = hasher.finalize();

    Ok(Sha2Value::new(&h))
}

/*
/// Hash a string slice using the given hasher as a Digest implementation
/// Returns a `Sha2Value`, which is a wrapper around a [u8; 32]
/// # Errors
/// Will return an error if the file cannot be opened or read
pub fn hash_str<D: Digest>(text: &str) -> anyhow::Result<Sha2Value> {
    if text.is_empty() {
        return Ok(Sha2Value::default());
    }

    let h = D::new().chain_update(text).finalize();

    Ok(Sha2Value::new(&h))
}
*/

/// Check for unused arguments, and error out if there are any
pub fn args_finished(args: pico_args::Arguments) -> anyhow::Result<()> {
    let unused = args.finish();
    if unused.is_empty() {
        Ok(())
    } else {
        Err(anyhow::anyhow!("Unused arguments: {:?}", unused))
    }
}
