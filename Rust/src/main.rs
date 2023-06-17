//#![allow(unused_imports)]
//#![allow(dead_code)]
//#![allow(unused_variables)]

#[allow(clippy::wildcard_imports)]
use filedata::*;
use std::collections::HashSet;
use std::hash::Hash;
use std::marker::PhantomData;
use std::path::Path;
#[allow(clippy::wildcard_imports)]
use utils::*;
use walkdir::WalkDir;

mod filedata;
mod utils;

fn main() -> anyhow::Result<()> {
    let mut pargs = pico_args::Arguments::from_env();
    let raw = pargs.contains(["-r", "--raw"]);
    if !raw {
        println!(
            "Rust Folder Comparer, ver: {}, commit: {}",
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
    let firstonly = pargs.contains(["-f", "--first-only"]);

    if compareropt.is_err() {
        return Err(anyhow::anyhow!(
            "Comparison should be Name, NameSize or Hash"
        ));
    }

    // these give "\\?\C:\Users\JohnT\Documents\junk\2", not ideal
    let folder1 = Path::new(&path1).canonicalize()?;
    let folder2 = Path::new(&path2).canonicalize()?;
    let comparer = compareropt.unwrap();

    if folder1 == folder2 {
        return Err(anyhow::anyhow!("Folders should not be the same"));
    }

    if !raw {
        println!(
            "Comparing folders '{}' and '{}', comparing by {:?}",
            folder1.display(),
            folder2.display(),
            comparer
        );
        println!();
    }

    // call the appropriate comparison function
    // this is a bit ugly but HashSet doesnt have pluggable comparers
    match comparer {
        FileDataCompareOption::Name => {
            scan_and_check::<UniqueName>(&folder1, &folder2, comparer, raw, firstonly)?;
        }
        FileDataCompareOption::NameSize => {
            scan_and_check::<UniqueNameSize>(&folder1, &folder2, comparer, raw, firstonly)?;
        }
        FileDataCompareOption::Hash => {
            scan_and_check::<UniqueHash>(&folder1, &folder2, comparer, raw, firstonly)?;
        }
    }

    Ok(())
}

/// Wrapper around main scanning and comparison. Only needed because this is generic over the comparison type U
fn scan_and_check<U>(
    folder1: &Path,
    folder2: &Path,
    comparer: FileDataCompareOption,
    raw: bool,
    firstonly: bool,
) -> anyhow::Result<()>
where
    FileData<U>: Eq + Hash, // FileData<U> must be properly comparable
    U: UniqueTrait,         // U must be a Unique key marker
{
    // scan the folders and populate the HashSets
    let files1 = scan_folder::<U>(folder1, comparer)?;
    let files2 = scan_folder::<U>(folder2, comparer)?;

    // find whats in files1, but not in files2
    let diff1: Vec<_> = files1.difference(&files2).collect();
    show_results(&diff1, folder1, folder2, raw);

    if !firstonly {
        // find whats in files2, but not in files1
        let diff2: Vec<_> = files2.difference(&files1).collect();
        show_results(&diff2, folder2, folder1, raw);
    }

    Ok(())
}

/// Show the results of the comparison
fn show_results<U: UniqueTrait>(diff: &Vec<&FileData<U>>, dir1: &Path, dir2: &Path, raw: bool) {
    if !raw {
        println!(
            "Files in '{}' but not in '{}'",
            dir1.display(),
            dir2.display()
        );
        if diff.is_empty() {
            println!("None");
        }
    }
    for f in diff {
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
                    Some(hash_file::<sha2::Sha256>(fpath.as_str())?)
                } else {
                    None
                },
                path: fpath, // needs to come after hash because it consumes fpath
                phantom: PhantomData,
            });
        }
    }

    Ok(fileset)
}
