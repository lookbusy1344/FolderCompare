use std::{collections::HashSet, time::Instant};

use customhashset::get_hash;
use customhashset::CustomHashSet;
use rand::{thread_rng, Rng};
mod customhashset;

const INT_ITERATIONS: usize = 2_000_000;
const STR_ITERATIONS: usize = 500_000;
const ARRAY_ITERATIONS: usize = 500_000;

const NUM_BUCKETS: usize = 20_000;
const BUCKET_CAPACITY: usize = 50;

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
struct SmallArray {
    data: [u8; 32],
}

fn main() {
    int_hash();
    string_hash();
    array_hash();
}

fn int_hash() {
    let mut normal_hashset: HashSet<i32> = HashSet::new();
    let mut custom_hashset = CustomHashSet::new(eq_int, hash_int, NUM_BUCKETS, BUCKET_CAPACITY);

    // generate 100000 random numbers
    let mut rng = thread_rng();
    let mut numbers = Vec::with_capacity(INT_ITERATIONS);

    // build an array of random items
    for _ in 0..INT_ITERATIONS {
        let r: i32 = rng.gen();
        numbers.push(r);
    }

    // ===== Test normal hash set =====

    let normal_start = Instant::now();
    // insert the items into the hash set
    for i in &numbers {
        normal_hashset.insert(*i);
    }
    let normal_duration = normal_start.elapsed();

    // ===== Test custom hash set =====

    let custom_start = Instant::now();
    // insert the items into the hash set
    for i in &numbers {
        custom_hashset.insert(*i);
    }
    let custom_duration = custom_start.elapsed();

    println!();
    println!("Int iterations {INT_ITERATIONS}");
    println!(
        "Smallest bucket size: {}",
        custom_hashset.smallest_bucket_size()
    );
    println!(
        "Largest bucket size: {}",
        custom_hashset.largest_bucket_size()
    );
    println!("Count: {}", custom_hashset.len());
    println!("Normal count: {}", normal_hashset.len());
    if custom_hashset.len() != normal_hashset.len() {
        println!("WARNING: Custom and normal hash sets are not the same size");
    }

    println!("Custom insert took {custom_duration:?}");
    println!("Normal insert took {normal_duration:?}");

    let factor = custom_duration.as_nanos() as f64 / normal_duration.as_nanos() as f64;
    println!("Custom insert was {factor:.2} times slower than normal");
}

fn string_hash() {
    let mut normal_hashset: HashSet<String> = HashSet::new();
    let mut custom_hashset =
        CustomHashSet::new(eq_string, hash_string, NUM_BUCKETS, BUCKET_CAPACITY);

    // generate 100000 random numbers
    let mut rng = thread_rng();
    let mut numbers = Vec::with_capacity(STR_ITERATIONS);

    // build an array of random items
    for _ in 0..STR_ITERATIONS {
        let r: u64 = rng.gen();
        numbers.push(r.to_string());
    }

    // ===== Test normal hash set =====

    let normal_start = Instant::now();
    // insert the items into the hash set
    for i in &numbers {
        normal_hashset.insert(i.clone());
    }
    let normal_duration = normal_start.elapsed();

    // ===== Test custom hash set =====

    let custom_start = Instant::now();
    // insert the items into the hash set
    for i in &numbers {
        custom_hashset.insert(i.clone());
    }
    let custom_duration = custom_start.elapsed();

    println!();
    println!("Str iterations {STR_ITERATIONS} ");
    println!(
        "Smallest bucket size: {}",
        custom_hashset.smallest_bucket_size()
    );
    println!(
        "Largest bucket size: {}",
        custom_hashset.largest_bucket_size()
    );
    println!("Count: {}", custom_hashset.len());
    println!("Normal count: {}", normal_hashset.len());
    if custom_hashset.len() != normal_hashset.len() {
        println!("WARNING: Custom and normal hash sets are not the same size");
    }

    println!("Custom insert took {custom_duration:?}");
    println!("Normal insert took {normal_duration:?}");

    let factor = custom_duration.as_nanos() as f64 / normal_duration.as_nanos() as f64;
    println!("Custom insert was {factor:.2} times slower than normal");
}

fn array_hash() {
    let mut normal_hashset: HashSet<SmallArray> = HashSet::new();
    let mut custom_hashset = CustomHashSet::new(eq_array, hash_array, NUM_BUCKETS, BUCKET_CAPACITY);

    // generate 100000 random numbers
    let mut rng = thread_rng();
    let mut data = Vec::with_capacity(ARRAY_ITERATIONS);

    // build an array of random items
    for _ in 0..ARRAY_ITERATIONS {
        let mut array: [u8; 32] = [0; 32];
        rng.fill(&mut array[..]);
        data.push(SmallArray { data: array });
    }

    // ===== Test normal hash set =====

    let normal_start = Instant::now();
    // insert the items into the hash set
    for i in &data {
        normal_hashset.insert(i.clone());
    }
    let normal_duration = normal_start.elapsed();

    // ===== Test custom hash set =====

    let custom_start = Instant::now();
    // insert the items into the hash set
    for i in &data {
        custom_hashset.insert(i.clone());
    }
    let custom_duration = custom_start.elapsed();

    println!();
    println!("Array iterations {ARRAY_ITERATIONS}");
    println!(
        "Smallest bucket size: {}",
        custom_hashset.smallest_bucket_size()
    );
    println!(
        "Largest bucket size: {}",
        custom_hashset.largest_bucket_size()
    );
    println!("Count: {}", custom_hashset.len());
    println!("Normal count: {}", normal_hashset.len());
    if custom_hashset.len() != normal_hashset.len() {
        println!("WARNING: Custom and normal hash sets are not the same size");
    }

    println!("Custom insert took {custom_duration:?}");
    println!("Normal insert took {normal_duration:?}");

    let factor = custom_duration.as_nanos() as f64 / normal_duration.as_nanos() as f64;
    println!("Custom insert was {factor:.2} times slower than normal");
}

// ===== Int hashing functions =====

fn eq_int(a: &i32, b: &i32) -> bool {
    *a == *b
}

fn hash_int(a: &i32) -> usize {
    *a as usize
}

// ===== String hashing functions =====

fn eq_string(a: &String, b: &String) -> bool {
    *a == *b
}

fn hash_string(a: &String) -> usize {
    get_hash(a)
}

// ===== Array hashing functions =====

fn eq_array(a: &SmallArray, b: &SmallArray) -> bool {
    a.data == b.data
}

fn hash_array(a: &SmallArray) -> usize {
    let (int_bytes, _) = a.data.split_at(std::mem::size_of::<usize>());
    usize::from_be_bytes(int_bytes.try_into().unwrap())
}
