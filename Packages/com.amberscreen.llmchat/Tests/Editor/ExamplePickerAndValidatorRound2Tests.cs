using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace AmberScreen.LLMChat.Tests
{
    public sealed class ExamplePickerAndValidatorRound2Tests
    {
        [Test]
        public void RoleplayMarkup_Rejected_PlainAccepted()
        {
            Assert.That(
                LLMChatReplyValidator.TryValidate(
                    "* sends gif of a sinking boat **naaaah**",
                    "friend_a",
                    new List<LLMChatHistoryTurn>(),
                    out string reason),
                Is.False);
            Assert.That(reason, Is.EqualTo(LLMChatReplyValidator.ReasonRoleplayMarkup));

            Assert.That(
                LLMChatReplyValidator.TryValidate("NAHHH", "friend_a", new List<LLMChatHistoryTurn>(), out _),
                Is.True);
            Assert.That(
                LLMChatReplyValidator.TryValidate("yeah.", "friend_a", new List<LLMChatHistoryTurn>(), out _),
                Is.True);
            // Paired _..._ removed — snake_case must pass.
            Assert.That(
                LLMChatReplyValidator.TryValidate(
                    "check the file_name file later",
                    "friend_b",
                    new List<LLMChatHistoryTurn>(),
                    out _),
                Is.True);
        }

        [Test]
        public void RepeatOpener_ThirdHaha_Rejected_VariedPass()
        {
            var history = new List<LLMChatHistoryTurn>
            {
                new LLMChatHistoryTurn("friend_c", "Alex", "haha no stress all good", false),
                new LLMChatHistoryTurn("player", "Player", "hey", true),
                new LLMChatHistoryTurn("friend_c", "Alex", "haha yeah on shift till 2", false),
            };

            Assert.That(
                LLMChatReplyValidator.TryValidate(
                    "haha classic mate",
                    "friend_c",
                    history,
                    out string reason),
                Is.False);
            Assert.That(reason, Is.EqualTo(LLMChatReplyValidator.ReasonRepeatOpener));

            Assert.That(
                LLMChatReplyValidator.TryValidate(
                    "sorted, ill ring him",
                    "friend_c",
                    history,
                    out _),
                Is.True);
        }

        [Test]
        public void ExamplePicker_NoDuplicateOpeners_InOnePick()
        {
            var lines = new[]
            {
                "haha nah all good",
                "haha classic",
                "sorted",
                "bro what",
                "ok ok lets not",
                "lads who's out",
                "on shift till 2",
                "its fine its fine",
            };
            var pick = LLMChatExamplePicker.Pick(lines, null, "low", new System.Random(7));
            Assert.That(pick.Count, Is.GreaterThanOrEqualTo(LLMChatExamplePicker.MinPick));
            var openers = pick.Select(LLMChatReplyValidator.FirstWord).ToList();
            Assert.That(openers.Distinct().Count(), Is.EqualTo(openers.Count),
                "duplicate opener in pick: " + string.Join(" | ", pick));
        }

        [Test]
        public void ExamplePicker_HighTension_PrefersTellSamples()
        {
            var lines = new[]
            {
                "GOOD MORNING TO EVERYONE EXCEPT THEM",
                "LMAOOO",
                "moneybags has entered the chat",
                "NAHHH",
                "who's paying. asking for a friend",
                "IM CRYING",
                "it was a joke",
                "yeah.",
                "fine.",
            };
            var tags = new[]
            {
                "low", "low", "low", "low", "low", "low", "tell", "tell", "tell",
            };
            var pick = LLMChatExamplePicker.Pick(lines, tags, "high", new System.Random(3));
            Assert.That(pick, Does.Contain("it was a joke").Or.Contain("yeah.").Or.Contain("fine."));
            int tellHits = pick.Count(p => p == "it was a joke" || p == "yeah." || p == "fine.");
            Assert.That(tellHits, Is.GreaterThanOrEqualTo(1));
        }
    }
}
