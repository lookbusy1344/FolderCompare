//#![allow(unused_imports)]
//#![allow(dead_code)]
//#![allow(unused_variables)]

use customhashset::CustomHashSet;
#[allow(clippy::wildcard_imports)]
use filedata::*;
use std::collections::HashSet;
use std::hash::Hash;
use std::marker::PhantomData;
use std::path::Path;
#[allow(clippy::wildcard_imports)]
use utils::*;
use walkdir::WalkDir;

mod customhashset;
mod filedata;
mod utils;

fn main() -> anyhow::Result<()> {
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
        return Ok(());
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

    // comparing a folder with itself is pointless
    if config.folder1 == config.folder2 {
        return Err(anyhow::anyhow!("Folders should not be the same"));
    }

    if !config.raw {
        println!(
            "Comparing folders '{}' and '{}'. Comparing by {:?}",
            config.folder1.display(),
            config.folder2.display(),
            config.comparer
        );
        println!();
    }

    // call the appropriate comparison function
    // this is a bit ugly but HashSet doesnt have pluggable comparers
    match config.comparer {
        FileDataCompareOption::Name => {
            scan_and_check::<UniqueName>(&config)?;
        }
        FileDataCompareOption::NameSize => {
            scan_and_check::<UniqueNameSize>(&config)?;
        }
        FileDataCompareOption::Hash => {
            scan_and_check::<UniqueHash>(&config)?;
        }
    }

    Ok(())
}

/// Wrapper around main scanning and comparison. Only needed because this is generic over the comparison type U
fn scan_and_check<U>(config: &Config) -> anyhow::Result<()>
where
    FileData<U>: Eq + Hash, // FileData<U> must be properly comparable
    U: UniqueTrait,         // U must be a Unique key marker
{
    // scan the folders and populate the HashSets
    let files1;
    let files2;
    if config.onethread {
        // scan the two folders in series, using one thread
        files1 = scan_folder::<U>(&config.folder1, config.comparer)?;
        files2 = scan_folder::<U>(&config.folder2, config.comparer)?;
    } else {
        // scan them in parallel
        let (resfiles1, resfiles2) = rayon::join(
            || scan_folder::<U>(&config.folder1, config.comparer),
            || scan_folder::<U>(&config.folder2, config.comparer),
        );

        files1 = resfiles1?;
        files2 = resfiles2?;
    }

    // find whats in files1, but not in files2
    let diff1: Vec<_> = files1.difference(&files2).collect();
    show_results(&diff1, &config.folder1, &config.folder2, config.raw);

    // count the differences
    let count = if config.firstonly {
        // we dont care about the second stage, just yield the first count
        diff1.len()
    } else {
        // find whats in files2, but not in files1
        let diff2: Vec<_> = files2.difference(&files1).collect();
        show_results(&diff2, &config.folder2, &config.folder1, config.raw);

        // yield both counts
        diff1.len() + diff2.len()
    };

    if !config.raw {
        println!("{count} difference(s) found");
    }

    Ok(())
}

/// Show the results of the comparison
fn show_results<U: UniqueTrait>(
    differences: &Vec<&FileData<U>>,
    presentindir: &Path,
    absentindir: &Path,
    raw: bool,
) {
    if !raw {
        println!(
            "Files in '{}' but not in '{}'",
            presentindir.display(),
            absentindir.display()
        );
        if differences.is_empty() {
            println!("None");
        }
    }
    for f in differences {
        println!("{}", f.path);
    }
    if !raw {
        println!();
    }
}

/// Scan a folder and return a set of files
fn scan_folder<U>(
    dir: &Path,
    comparer: FileDataCompareOption,
) -> anyhow::Result<HashSet<FileData<U>>>
where
    FileData<U>: Eq + Hash, // FileData<U> must be properly comparable
    U: UniqueTrait,         // U must be a Unique key marker
{
    let mut fileset: HashSet<FileData<U>> = HashSet::with_capacity(200);

    for entry in WalkDir::new(dir).into_iter().filter_map(Result::ok) {
        if entry.file_type().is_file() {
            let fname = entry.file_name().to_str().unwrap().to_string();
            let fpath = entry.path().to_str().unwrap().to_string();
            let fsize = entry.metadata().unwrap().len();

            fileset.insert(FileData::<U> {
                filename: fname,
                size: fsize,
                hash: if comparer == FileDataCompareOption::Hash {
                    hash_file::<sha2::Sha256>(fpath.as_str())?
                } else {
                    Sha2Value::default()
                },
                path: fpath, // needs to come after hash because it consumes fpath
                phantom: PhantomData,
            });
        }
    }

    Ok(fileset)
}

/// Scan a folder and populates the given hashset with the files
fn scan_folder2(
    dir: &Path,
    needs_hash: bool,
    fileset: &mut CustomHashSet<FileData2>,
) -> anyhow::Result<()> {
    for entry in WalkDir::new(dir).into_iter().filter_map(Result::ok) {
        if entry.file_type().is_file() {
            let fname = entry.file_name().to_str().unwrap().to_string();
            let fpath = entry.path().to_str().unwrap().to_string();
            let fsize = entry.metadata().unwrap().len();

            fileset.insert(FileData2 {
                filename: fname,
                size: fsize,
                hash: if needs_hash {
                    hash_file::<sha2::Sha256>(fpath.as_str())?
                } else {
                    Sha2Value::default()
                },
                path: fpath, // needs to come after hash because it consumes fpath
            });
        }
    }

    Ok(())
}
