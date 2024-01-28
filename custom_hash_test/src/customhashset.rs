#![allow(dead_code)]

use std::{
    collections::hash_map::DefaultHasher,
    hash::{Hash, Hasher},
};

// types for equality and hash functions
pub type EqualityFn<T> = fn(&T, &T) -> bool;
pub type HashFn<T> = fn(&T) -> usize;

/// A simple custom hash set that takes two lambda expressions for equality and hashing
pub struct CustomHashSet<T> {
    /// use a vector of vectors as the underlying data structure
    buckets: Vec<Vec<T>>,
    /// store the capacity of the buckets
    buckets_required: usize,
    /// store the equality function as a field
    eq_fn: EqualityFn<T>,
    /// store the hash function as a field
    hash_fn: HashFn<T>,
}

impl<T> CustomHashSet<T> {
    // Create a new CustomHashSet with the given equality and hash functions and an initial capacity
    pub fn new(
        eq_fn: EqualityFn<T>,
        hash_fn: HashFn<T>,
        buckets_required: usize,
        default_bucket_size: usize,
    ) -> Self {
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

    /// Find the the values that are in self but not in other. Returns a vecto
    pub fn difference(&self, other: &Self) -> Vec<&T> {
        let mut diff = Vec::with_capacity(100);
        for item in self.iter() {
            if !other.contains(item) {
                diff.push(item);
            }
        }
        diff
    }

    /// Find the the values that are in self but not in other. Returns an iterator
    pub fn difference_iter<'a, 'b: 'a>(
        &'a self,
        other: &'b Self,
    ) -> impl Iterator<Item = &'a T> + 'a {
        // 'b must live at least as long as 'a, because we are still walking the iterator after this 'returns'
        // whereas the vector version returns a vector that is not dependent on the lifetime of 'b
        self.iter().filter(move |item| !other.contains(item))
    }

    /// Rebuild the hashset with given buckets and sizes
    pub fn rebuild(&mut self, buckets_required: usize, default_bucket_size: usize) {
        let mut new_buckets = create_buckets(buckets_required, default_bucket_size);

        // take ownership of the old buckets. self.buckets is now an empty vector
        let old_buckets = std::mem::take(&mut self.buckets);

        for mut bucket in old_buckets {
            for item in bucket.drain(..) {
                // for each item in the bucket, get the hash and the index of the new bucket
                // doesn't require equality check, because the source items are unique
                let hash = (self.hash_fn)(&item);
                let index = hash % buckets_required;
                new_buckets[index].push(item);
            }
        }

        // replace the empty buckets with the new buckets
        self.buckets = new_buckets;
        self.buckets_required = buckets_required;
    }

    /// Iterate over the hash set
    #[inline]
    pub fn iter(&self) -> impl Iterator<Item = &T> {
        self.buckets.iter().flatten()
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
    pub fn get_buckets(&self) -> &Vec<Vec<T>> {
        &self.buckets
    }

    /// Get the largest current bucket size
    pub fn largest_bucket_size(&self) -> usize {
        self.buckets
            .iter()
            .map(std::vec::Vec::len)
            .max()
            .unwrap_or(0)
    }

    /// Get the smallest current bucket size
    pub fn smallest_bucket_size(&self) -> usize {
        self.buckets
            .iter()
            .map(std::vec::Vec::len)
            .min()
            .unwrap_or(0)
    }

    /// Get the number of buckets that are empty
    pub fn empty_buckets(&self) -> usize {
        self.buckets
            .iter()
            .filter(|bucket| bucket.is_empty())
            .count()
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
