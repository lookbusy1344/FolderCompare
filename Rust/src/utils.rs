use git_version::git_version;
use sha2::Digest;
use std::fs::File;
use std::io::{BufReader, Read};
use std::path::{Path, PathBuf};

use crate::filedata::{FileDataCompareOption, Sha2Value};
use crate::parse_comparer;

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
        return Err(anyhow::anyhow!(""));
    }

    let path1: String = pargs.value_from_str(["-a", "--foldera"])?;
    let path2: String = pargs.value_from_str(["-b", "--folderb"])?;
    let compstr: Option<String> = pargs.opt_value_from_str(["-c", "--comparison"])?;
    let compareropt = parse_comparer(&compstr);

    if compareropt.is_err() {
        return Err(anyhow::anyhow!(
            "Comparison should be Name, NameSize or Hash"
        ));
    }

    // package the config options, so they can be easily passed around
    let config = Config {
        folder1: Path::new(&path1).canonicalize()?,
        folder2: Path::new(&path2).canonicalize()?,
        comparer: compareropt.unwrap(),
        raw,
        firstonly: pargs.contains(["-f", "--first-only"]),
        onethread: pargs.contains(["-o", "--one-thread"]),
    };

    // Check for unused arguments, and error out if there are any
    args_finished(pargs)?;

    Ok(config)
}

/// Check for unused arguments, and error out if there are any
fn args_finished(args: pico_args::Arguments) -> anyhow::Result<()> {
    let unused = args.finish();
    if unused.is_empty() {
        Ok(())
    } else {
        Err(anyhow::anyhow!("Unused arguments: {:?}", unused))
    }
}
