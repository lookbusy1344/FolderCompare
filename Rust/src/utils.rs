use base64::{engine::general_purpose, Engine as _};
use git_version::git_version;
use sha2::Digest;
use std::fs::File;
use std::io::{BufReader, Read};

pub const VERSION: Option<&str> = option_env!("CARGO_PKG_VERSION");
pub const GIT_VERSION: &str = git_version!();

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

/// Hash a file using the given hasher as a Digest implementation
/// Returns base64 encoded hash
/// # Errors
/// Will return an error if the file cannot be opened or read
pub fn hash_file<D: Digest>(filename: &str) -> anyhow::Result<String> {
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

    // originally used hex::encode(h), from the hex crate (base 16)
    // this is base64 encoding, which is shorter and faster
    Ok(general_purpose::STANDARD_NO_PAD.encode(h))
}
