﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Umbraco.Core;

namespace Umbraco.Tests.CoreThings
{
    [TestFixture]
    public class EnumerableExtensionsTests
    {
        [Test]
        public void Unsorted_Sequence_Equal()
        {
            var list1 = new[] { 1, 2, 3, 4, 5, 6 };
            var list2 = new[] { 6, 5, 3, 2, 1, 4 };
            var list3 = new[] { 6, 5, 4, 3, 2, 2 };

            Assert.IsTrue(list1.UnsortedSequenceEqual(list2));
            Assert.IsTrue(list2.UnsortedSequenceEqual(list1));
            Assert.IsFalse(list1.UnsortedSequenceEqual(list3));

            Assert.IsTrue(((IEnumerable<object>)null).UnsortedSequenceEqual(null));
            Assert.IsFalse(((IEnumerable<int>)null).UnsortedSequenceEqual(list1));
            Assert.IsFalse(list1.UnsortedSequenceEqual(null));
        }

        [Test]
        public void Contains_All()
        {
            var list1 = new[] {1, 2, 3, 4, 5, 6};
            var list2 = new[] {6, 5, 3, 2, 1, 4};
            var list3 = new[] {6, 5, 4, 3};

            Assert.IsTrue(list1.ContainsAll(list2));
            Assert.IsTrue(list2.ContainsAll(list1));
            Assert.IsTrue(list1.ContainsAll(list3));
            Assert.IsFalse(list3.ContainsAll(list1));
        }

        [Test]
        public void SelectRecursive_2()
        {
            var hierarchy = new TestItem("1")
            {
                    Children = new List<TestItem>
                    {
                            new TestItem("1.1"),
                            new TestItem("1.2"),
                            new TestItem("1.3")
                        }
                };

#pragma warning disable CS0618 // Type or member is obsolete
            var flattened = FlattenList(hierarchy.Children, x => x.Children);
#pragma warning restore CS0618 // Type or member is obsolete
            var selectRecursive = hierarchy.Children.SelectRecursive(x => x.Children);

            Assert.AreEqual(3, flattened.Count());
            Assert.AreEqual(3, selectRecursive.Count());

            Assert.IsTrue(flattened.SequenceEqual(selectRecursive));
        }

        [Test]
        public void SelectRecursive()
        {
            var hierarchy = new TestItem("1")
            {
                Children = new List<TestItem>
                {
                    new TestItem("1.1")
                    {
                        Children = new List<TestItem>
                        {
                            new TestItem("1.1.1")
                            {
                                Children = new List<TestItem>
                                {
                                    new TestItem("1.1.1.1")
                                    {
                                        Children = new List<TestItem>
                                        {
                                            new TestItem("1.1.1.1.1"),
                                            new TestItem("1.1.1.1.2")
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new TestItem("1.2")
                    {
                        Children = new List<TestItem>
                        {
                            new TestItem("1.2.1")
                            {
                                Children = new List<TestItem>
                                {
                                    new TestItem("1.2.1.1")
                                    {
                                        Children = new List<TestItem>()
                                    }
                                }
                            },
                            new TestItem("1.2.2")
                            {
                                Children = new List<TestItem>
                                {
                                    new TestItem("1.2.2.1")
                                    {
                                        Children = new List<TestItem>()
                                    }
                                }
                            }
                        }
                    }
                }
            };

#pragma warning disable CS0618 // Type or member is obsolete
            var flattened = FlattenList(hierarchy.Children, x => x.Children);
#pragma warning restore CS0618 // Type or member is obsolete
            var selectRecursive = hierarchy.Children.SelectRecursive(x => x.Children);

            Assert.AreEqual(10, flattened.Count());
            Assert.AreEqual(10, selectRecursive.Count());

            // both methods return the same elements, but not in the same order
            Assert.IsFalse(flattened.SequenceEqual(selectRecursive));
            foreach (var x in flattened) Assert.IsTrue(selectRecursive.Contains(x));
        }

        private class TestItem
        {
            public TestItem(string name)
            {
                Children = Enumerable.Empty<TestItem>();
                Name = name;
            }
            public string Name { get; }
            public IEnumerable<TestItem> Children { get; set; }
        }



        private IEnumerable<T> FlattenList<T>(IEnumerable<T> e, Func<T, IEnumerable<T>> f)
        {
            return e.SelectMany(c => FlattenList(f(c), f)).Concat(e);
        }

        [Test]
        public void InGroupsOf_ReturnsAllElements()
        {
            var integers = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

            var groupsOfTwo = integers.InGroupsOf(2).ToArray();

            var flattened = groupsOfTwo.SelectMany(x => x).ToArray();

            Assert.That(groupsOfTwo.Length, Is.EqualTo(5));
            Assert.That(flattened.Length, Is.EqualTo(integers.Length));
            CollectionAssert.AreEquivalent(integers, flattened);

            var groupsOfMassive = integers.InGroupsOf(100).ToArray();
            Assert.That(groupsOfMassive.Length, Is.EqualTo(1));
            flattened = groupsOfMassive.SelectMany(x => x).ToArray();
            Assert.That(flattened.Length, Is.EqualTo(integers.Length));
            CollectionAssert.AreEquivalent(integers, flattened);
        }

        [Test]
        public void InGroupsOf_CanRepeat()
        {
            var integers = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
            var inGroupsOf = integers.InGroupsOf(2);
            Assert.AreEqual(5, inGroupsOf.Count());
            Assert.AreEqual(5, inGroupsOf.Count()); // again
        }

        [TestCase]
        public void DistinctBy_ReturnsDistinctElements_AndResetsIteratorCorrectly()
        {
            // Arrange
            var tuple1 = new System.Tuple<string, string>("fruit", "apple");
            var tuple2 = new System.Tuple<string, string>("fruit", "orange");
            var tuple3 = new System.Tuple<string, string>("fruit", "banana");
            var tuple4 = new System.Tuple<string, string>("fruit", "banana"); // Should be filtered out
            var list = new List<System.Tuple<string, string>>()
                {
                    tuple1,
                    tuple2,
                    tuple3,
                    tuple4
                };

            // Act
            var iteratorSource = list.DistinctBy(x => x.Item2);

            // Assert
            // First check distinction
            Assert.AreEqual(3, iteratorSource.Count());

            // Check for iterator block mistakes - reset to original query first
            iteratorSource = list.DistinctBy(x => x.Item2);
            Assert.AreEqual(iteratorSource.Count(), iteratorSource.ToList().Count());
        }
    }
}
