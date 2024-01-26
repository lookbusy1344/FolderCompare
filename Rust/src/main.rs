//#![allow(unused_imports)]
//#![allow(dead_code)]
//#![allow(unused_variables)]

#[allow(clippy::wildcard_imports)]
use filedata::*;
use std::{collections::HashSet, path::Path};
#[allow(clippy::wildcard_imports)]
use utils::*;
use walkdir::WalkDir;

mod filedata;
mod utils;

fn main() -> anyhow::Result<()> {
    // parse the command line arguments
    let config = parse_args()?;

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

    scan_and_check(&config)?;

    Ok(())
}

/// Wrapper around main scanning and comparison. Only needed because this is generic over the comparison type U
fn scan_and_check(config: &Config) -> anyhow::Result<()> {
    // create the hashsets
    let files1;
    let files2;

    // scan the folders and populate the HashSets
    if config.onethread {
        // scan the two folders in series, using one thread
        files1 = scan_folder(config, &config.folder1)?;
        files2 = scan_folder(config, &config.folder2)?;
    } else {
        // scan them in parallel
        let (resfiles1, resfiles2) = rayon::join(
            || scan_folder(config, &config.folder1),
            || scan_folder(config, &config.folder2),
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

        // *** hashset stats ***
        // let lbs1 = files1.largest_bucket_size();
        // let lbs2 = files2.largest_bucket_size();
        // let empty1 = files1.empty_buckets();
        // let empty2 = files2.empty_buckets();
        // let size1 = files1.len();
        // let size2 = files2.len();

        // println!("Folder1: {size1} files, largest bucket size {lbs1}, empty buckets {empty1}");
        // println!("Folder2: {size2} files, largest bucket size {lbs2}, empty buckets {empty2}");
    }

    Ok(())
}

/// Show the results of the comparison
fn show_results(differences: &Vec<&FileData>, presentindir: &Path, absentindir: &Path, raw: bool) {
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

/// Scan a folder and build hashset with the files
fn scan_folder(config: &Config, dir: &Path) -> anyhow::Result<HashSet<FileData>> {
    let mut fileset = make_hashset(config);
    let include_sha2 = config.comparer == FileDataCompareOption::Hash;

    for entry in WalkDir::new(dir).into_iter().filter_map(Result::ok) {
        if entry.file_type().is_file() {
            let fname = entry.file_name().to_str().unwrap().to_string();
            let fpath = entry.path().to_str().unwrap().to_string();
            let fsize = entry.metadata().unwrap().len();

            fileset.insert(FileData {
                filename: fname,
                size: fsize,
                hash: if include_sha2 {
                    hash_file::<sha2::Sha256>(fpath.as_str())?
                } else {
                    Sha2Value::default()
                },
                path: fpath, // needs to come after hash because it consumes fpath
            });
        }
    }

    Ok(fileset)
}

/// Make a hashset with the given comparison lambdas
fn make_hashset(config: &Config) -> HashSet<FileData> {
    HashSet::new()
    // match config.comparer {
    //     FileDataCompareOption::Name => CustomHashSet::<FileData>::new(
    //         eq_filename,
    //         hash_filename,
    //         config.buckets,
    //         DEFAULT_BUCKET_SIZE,
    //     ),
    //     FileDataCompareOption::NameSize => CustomHashSet::<FileData>::new(
    //         eq_filename_size,
    //         hash_filename_size,
    //         config.buckets,
    //         DEFAULT_BUCKET_SIZE,
    //     ),
    //     FileDataCompareOption::Hash => {
    //         CustomHashSet::<FileData>::new(eq_sha2, hash_sha2, config.buckets, DEFAULT_BUCKET_SIZE)
    //     }
    // }
}
