using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace AmberScreen.Navigation.Tests
{
    public sealed class RouteAndPaceNoteTests
    {
        private static RouteStop Stop(string id, float z) => new RouteStop(id, id, new Vector3(0, 0, z));
        private static NavigationRoute Route()
        {
            var route = new NavigationRoute();
            route.SetStops(new[] { Stop("a", 10), Stop("b", 20) });
            return route;
        }
        [Test] public void RouteStartsAtFirstStopAndCopiesList()
        {
            var input = new List<RouteStop> { Stop("a", 10) };
            var route = new NavigationRoute(); route.SetStops(input); input.Clear();
            Assert.That(route.NextStop.Id, Is.EqualTo("a"));
            Assert.That(route.Stops.Count, Is.EqualTo(1));
        }
        [Test] public void DistanceIsHorizontal()
        {
            var route = Route(); route.UpdatePosition(new Vector3(3, 100, 6), 0);
            Assert.That(route.DistanceToNextStop, Is.EqualTo(5).Within(0.0001f));
        }
        [Test] public void ReachedFiresOnceAndNeverAdvances()
        {
            var route = Route(); int reached = 0; route.Reached += _ => reached++;
            route.UpdatePosition(new Vector3(0, 0, 10));
            route.UpdatePosition(Vector3.zero); route.UpdatePosition(new Vector3(0, 0, 10));
            Assert.That(reached, Is.EqualTo(1)); Assert.That(route.NextStop.Id, Is.EqualTo("a"));
        }
        [Test] public void AdvanceFiresOldAndNewStopAndCompletes()
        {
            var route = Route(); var events = new List<string>();
            route.Advanced += stop => events.Add("past:" + stop.Id);
            route.NextStopChanged += stop => events.Add(stop == null ? "done" : stop.Id);
            Assert.That(route.Advance(), Is.True); Assert.That(route.Advance(), Is.True);
            Assert.That(route.Advance(), Is.False);
            CollectionAssert.AreEqual(new[] { "past:a", "b", "past:b", "done" }, events);
            Assert.That(route.DistanceToNextStop, Is.EqualTo(float.PositiveInfinity));
        }
        [Test] public void ResetRearmsReachedAndAcceptsOwnSnapshot()
        {
            var route = Route(); int reached = 0; route.Reached += _ => reached++;
            route.UpdatePosition(new Vector3(0, 0, 10)); route.SetStops(route.Stops);
            route.UpdatePosition(new Vector3(0, 0, 10)); Assert.That(reached, Is.EqualTo(2));
        }
        [Test] public void InvalidStopsLeaveExistingRouteIntact()
        {
            var route = Route();
            Assert.Throws<ArgumentException>(() => route.SetStops(new[] { Stop("x", 0), Stop("x", 1) }));
            Assert.Throws<ArgumentException>(() => route.SetStops(new RouteStop[] { null }));
            Assert.That(route.NextStop.Id, Is.EqualTo("a"));
        }
        [Test] public void EmptyRouteHasNoNextStop()
        {
            var route = Route(); route.SetStops(null);
            Assert.That(route.NextStop, Is.Null); Assert.That(route.Advance(), Is.False);
        }
        [Test] public void InvalidPositionCannotReachStop()
        {
            var route = Route(); int count = 0; route.Reached += _ => count++;
            route.UpdatePosition(new Vector3(float.NaN, 0, 10));
            Assert.That(count, Is.Zero);
        }
        [Test] public void ReachedRearmsForNextStop()
        {
            var route = Route(); int count = 0; route.Reached += _ => count++;
            route.UpdatePosition(new Vector3(0, 0, 10), 0); route.Advance();
            route.UpdatePosition(new Vector3(0, 0, 20), 0); Assert.That(count, Is.EqualTo(2));
        }
        private static PaceNoteFeed Feed()
        {
            var feed = new PaceNoteFeed { CoDriverPresent = true };
            feed.SetNotes(new[] { new PaceNote("Right 3", 10f) }); return feed;
        }
        [Test] public void DistanceNoteFiresOnlyOnceAtThreshold()
        {
            var feed = Feed(); int count = 0; feed.NoteFired += _ => count++;
            feed.Tick(9, Vector3.zero, 0); Assert.That(count, Is.Zero);
            feed.Tick(10, Vector3.zero, 0); feed.Tick(20, Vector3.zero, 0);
            Assert.That(count, Is.EqualTo(1)); Assert.That(feed.CurrentNote.Text, Is.EqualTo("Right 3"));
        }
        [Test] public void LargeProgressStepStillFires()
        {
            var feed = Feed(); feed.Tick(100, Vector3.zero, 0);
            Assert.That(feed.CurrentNote, Is.Not.Null);
        }
        [Test] public void WorldRadiusIsHorizontalAndIndependentOfProgress()
        {
            var feed = new PaceNoteFeed { CoDriverPresent = true };
            feed.SetNotes(new[] { new PaceNote("Crest", new Vector3(0, 0, 10), 2) });
            feed.Tick(float.NaN, new Vector3(0, 200, 7), 0); Assert.That(feed.CurrentNote, Is.Null);
            feed.Tick(float.NaN, new Vector3(0, 200, 8), 0); Assert.That(feed.CurrentNote.Text, Is.EqualTo("Crest"));
        }
        [Test] public void AbsentCoDriverNeverFiresAndSkipsPastNotes()
        {
            var feed = Feed(); feed.CoDriverPresent = false; int count = 0; feed.NoteFired += _ => count++;
            feed.Tick(11, Vector3.zero, 0); feed.CoDriverPresent = true; feed.Tick(12, Vector3.zero, 0);
            Assert.That(count, Is.Zero); Assert.That(feed.CurrentNote, Is.Null);
        }
        [Test] public void AbsentCoDriverConsumesWorldTriggersToo()
        {
            var feed = new PaceNoteFeed(); int count = 0; feed.NoteFired += _ => count++;
            feed.SetNotes(new[] { new PaceNote("Gate", Vector3.zero, 1) });
            feed.Tick(0, Vector3.zero, 0); feed.CoDriverPresent = true; feed.Tick(0, Vector3.zero, 0);
            Assert.That(count, Is.Zero);
        }
        [Test] public void DepartureClearsImmediatelyAndDoesNotResume()
        {
            var feed = Feed(); feed.Tick(10, Vector3.zero, 0); feed.CoDriverPresent = false;
            Assert.That(feed.CurrentNote, Is.Null); Assert.That(feed.RemainingSeconds, Is.Zero);
            feed.CoDriverPresent = true; Assert.That(feed.CurrentNote, Is.Null);
        }
        [Test] public void NoteExpiresInCallerSuppliedSeconds()
        {
            var feed = Feed(); feed.Tick(10, Vector3.zero, 0); feed.Tick(10, Vector3.zero, 3);
            Assert.That(feed.CurrentNote, Is.Not.Null); feed.Tick(10, Vector3.zero, 1);
            Assert.That(feed.CurrentNote, Is.Null);
        }
        [Test] public void ResetRearmsNotes()
        {
            var feed = Feed(); int count = 0; feed.NoteFired += _ => count++;
            feed.Tick(10, Vector3.zero, 0); feed.SetNotes(new[] { new PaceNote("Left", 10f) });
            feed.Tick(10, Vector3.zero, 0); Assert.That(count, Is.EqualTo(2));
        }
        [Test] public void MultipleCrossingsFireInAuthoredOrderLastTextWins()
        {
            var feed = Feed(); var texts = new List<string>(); feed.NoteFired += n => texts.Add(n.Text);
            feed.SetNotes(new[] { new PaceNote("First", 1f), new PaceNote("Second", 2f) });
            feed.Tick(3, Vector3.zero, 0); CollectionAssert.AreEqual(new[] { "First", "Second" }, texts);
            Assert.That(feed.CurrentNote.Text, Is.EqualTo("Second"));
        }
        [Test] public void InvalidProgressDoesNotFireDistanceNote()
        {
            var feed = Feed(); feed.Tick(float.NaN, Vector3.zero, 0); feed.Tick(float.PositiveInfinity, Vector3.zero, 0);
            Assert.That(feed.CurrentNote, Is.Null);
        }
        [Test] public void NegativeTimeDoesNotExtendNote()
        {
            var feed = Feed(); feed.Tick(10, Vector3.zero, 0); feed.Tick(10, Vector3.zero, -20);
            Assert.That(feed.RemainingSeconds, Is.EqualTo(4));
        }
        [Test] public void InvalidNoteParametersAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PaceNote("x", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PaceNote("x", Vector3.zero, float.NaN));
        }
        [Test] public void InvalidNotesDoNotClearCurrentFeed()
        {
            var feed = Feed(); feed.Tick(10, Vector3.zero, 0);
            Assert.Throws<ArgumentException>(() => feed.SetNotes(new PaceNote[] { null }));
            Assert.That(feed.CurrentNote.Text, Is.EqualTo("Right 3"));
        }
        [Test] public void ZeroDisplayTimeDoesNotLeaveCurrentText()
        {
            var feed = Feed(); feed.DisplaySeconds = 0; int count = 0; feed.NoteFired += _ => count++;
            feed.Tick(10, Vector3.zero, 0); Assert.That(count, Is.EqualTo(1)); Assert.That(feed.CurrentNote, Is.Null);
        }
    }
}
