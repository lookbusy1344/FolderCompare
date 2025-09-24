//#![allow(unused_imports)]
//#![allow(dead_code)]
//#![allow(unused_variables)]

#[allow(clippy::wildcard_imports)]
use filedata::*;
use std::{collections::HashMap, path::Path};
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
    if config.one_thread {
        // scan the two folders in series, using one thread
        files1 = scan_folder(config, &config.folder1)?;
        files2 = scan_folder(config, &config.folder2)?;
    } else {
        // scan them in parallel
        let (res_files_1, res_files_2) = rayon::join(
            || scan_folder(config, &config.folder1),
            || scan_folder(config, &config.folder2),
        );

        files1 = res_files_1?;
        files2 = res_files_2?;
    }

    // find what's in files1, but not in files2
    let diff1 = hashmap_difference(&files1, &files2);
    show_results(&diff1, &config.folder1, &config.folder2, config.raw);

    // count the differences
    let count = if config.first_only {
        // we don't care about the second stage, just yield the first count
        diff1.len()
    } else {
        // find what's in files2, but not in files1
        let diff2 = hashmap_difference(&files2, &files1);
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
fn show_results(
    differences: &Vec<&FilePath>,
    present_in_dir: &Path,
    absent_in_dir: &Path,
    raw: bool,
) {
    if !raw {
        println!(
            "Files in '{}' but not in '{}'",
            present_in_dir.display(),
            absent_in_dir.display()
        );
        if differences.is_empty() {
            println!("None");
        }
    }
    for f in differences {
        println!("{f}");
    }
    if !raw {
        println!();
    }
}

/// Scan a folder and build hashset with the files
fn scan_folder(config: &Config, dir: &Path) -> anyhow::Result<HashMap<Sha2Hash, FilePath>> {
    let mut fileset: HashMap<Sha2Hash, FilePath> = HashMap::with_capacity(200);

    for entry in WalkDir::new(dir).into_iter().filter_map(Result::ok) {
        if entry.file_type().is_file() {
            let file_path = entry.path().to_str().unwrap();

            // generate the SHA2 key according to the comparison option
            let key = match config.comparer {
                FileDataCompareOption::Name => {
                    let file_name = entry.file_name().to_str().unwrap();
                    hash_string::<sha2::Sha256>(file_name)
                }
                FileDataCompareOption::NameSize => {
                    let file_name = entry.file_name().to_str().unwrap();
                    let file_size = entry.metadata()?.len();
                    hash_string_and_size::<sha2::Sha256>(file_name, file_size)
                }
                FileDataCompareOption::Hash => hash_file::<sha2::Sha256>(file_path)?,
            };

            // insert the file into the hashset, with required key
            fileset.insert(key, file_path.into());
        }
    }

    Ok(fileset)
}
