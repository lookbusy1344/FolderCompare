#![allow(dead_code)]

use std::{
    collections::hash_map::DefaultHasher,
    hash::{Hash, Hasher},
};

/// A simple custom hash set that takes two lambda expressions for equality and hashing
pub struct CustomHashSet<T, E, H> {
    /// use a vector of vectors as the underlying data structure
    buckets: Vec<Vec<T>>,
    /// store the capacity of the buckets
    buckets_required: usize,
    /// store the equality function as a field
    eq_fn: E,
    /// store the hash function as a field
    hash_fn: H,
}

impl<T, E, H> CustomHashSet<T, E, H>
where
    // constrain the equality type to be a function that takes two references to T and returns a bool
    E: Fn(&T, &T) -> bool,
    // constrain the hash type to be a function that takes a reference to T and returns a u64
    H: Fn(&T) -> usize,
{
    // Create a new CustomHashSet with the given equality and hash functions and an initial capacity
    pub fn new(eq_fn: E, hash_fn: H, buckets_required: usize, default_bucket_size: usize) -> Self {
        // create a vector of empty vectors with the given capacity
        let buckets = create_buckets(buckets_required, default_bucket_size);

        // return a new CustomHashSet with the buckets, capacity and functions
        Self {
            buckets,
            buckets_required,
            eq_fn,
            hash_fn,
        }
    }

    /// Insert an element into the `CustomHashSet`. Return true if the element was added
    pub fn insert(&mut self, value: T) -> bool {
        // use the hash function to get the hash of the value
        let hash = (self.hash_fn)(&value);

        // get the index of the bucket where the value should go
        // modulo by the capacity. This will truncate on 32-bit systems
        let index = hash % self.buckets_required;

        // get a reference to the bucket
        let bucket = &mut self.buckets[index];

        // check if the bucket already contains the value using the equality function
        if bucket.iter().any(|x| (self.eq_fn)(x, &value)) {
            // return false if the value was already present
            false
        } else {
            // push the value to the bucket and return true
            bucket.push(value);
            true
        }
    }

    /// Check if an element is present in the `CustomHashSet`
    pub fn contains(&self, value: &T) -> bool {
        // use the hash function to get the hash of the value
        let hash = (self.hash_fn)(value);

        // get the index of the bucket where the value should be
        // modulo by the capacity
        let index = hash % self.buckets_required;

        // get a reference to the bucket
        let bucket = &self.buckets[index];

        // check if the bucket contains the value using the equality function
        bucket.iter().any(|x| (self.eq_fn)(x, value))
    }

    /// Remove an element from the `CustomHashSet`. Returns true if the element was present
    pub fn remove(&mut self, value: &T) -> bool {
        // use the hash function to get the hash of the value
        let hash = (self.hash_fn)(value);

        // get the index of the bucket where the value should be
        // modulo by the capacity
        let index = hash % self.buckets_required;

        // get a mutable reference to the bucket
        let bucket = &mut self.buckets[index];

        // find the position of the value in the bucket using the equality function
        if let Some(pos) = bucket.iter().position(|x| (self.eq_fn)(x, value)) {
            // remove the value from the bucket and return true
            bucket.remove(pos);
            true
        } else {
            // return false if the value was not found
            false
        }
    }

    /// Length of hash set
    #[inline]
    pub fn len(&self) -> usize {
        self.buckets.iter().map(std::vec::Vec::len).sum()
    }

    /// Check if hash set is empty
    #[inline]
    pub fn is_empty(&self) -> bool {
        self.buckets.iter().all(std::vec::Vec::is_empty)
    }

    /// Clear the hash set
    pub fn clear(&mut self) {
        self.buckets.iter_mut().for_each(std::vec::Vec::clear);
    }

    // ==== DIAGNOSTICS =========================================================================

    /// Get a reference to the buckets
    //#[cfg(debug_assertions)]
    pub fn get_buckets(&self) -> &Vec<Vec<T>> {
        &self.buckets
    }

    /// Get the largest current bucket size
    //#[cfg(debug_assertions)]
    pub fn largest_bucket_size(&self) -> usize {
        self.buckets
            .iter()
            .map(std::vec::Vec::len)
            .max()
            .unwrap_or(0)
    }

    /// Get the smallest current bucket size
    //#[cfg(debug_assertions)]
    pub fn smallest_bucket_size(&self) -> usize {
        self.buckets
            .iter()
            .map(std::vec::Vec::len)
            .min()
            .unwrap_or(0)
    }
}

/// Helper to get the hash of a single value (including tuples)
#[inline]
#[allow(clippy::cast_possible_truncation)]
pub fn get_hash<T: Hash>(t: &T) -> usize {
    // get_hash("hello")
    // get_hash(&("hello", 99))
    let mut s = DefaultHasher::new();
    t.hash(&mut s);
    s.finish() as usize
}

/// Create a vector of empty buckets with the given capacity
fn create_buckets<T>(buckets_required: usize, default_bucket_size: usize) -> Vec<Vec<T>> {
    // vec![Vec::new(); buckets_required] requires T to implement Clone
    let mut buckets = Vec::with_capacity(buckets_required);
    for _ in 0..buckets_required {
        buckets.push(Vec::with_capacity(default_bucket_size));
    }
    buckets
}
