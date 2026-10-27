using System.Collections.Generic;
using NUnit.Framework;

namespace AmberScreen.LLMChat.Tests
{
    public sealed class CrossSpeakerRepeatTests
    {
        const string Line = "Sam's last seen is 22:58. Grey ticks.";

        [Test]
        public void OtherSpeakerIdentical_Rejected()
        {
            var history = new List<LLMChatHistoryTurn>
            {
                new LLMChatHistoryTurn("friend_b", "Sam", Line, false),
            };
            Assert.That(
                LLMChatReplyValidator.TryValidate(Line, "friend_a", history, out string reason),
                Is.False);
            Assert.That(reason, Is.EqualTo(LLMChatReplyValidator.ReasonCrossRepeat));
        }

        [Test]
        public void FactPlusTail_Rejected()
        {
            var history = new List<LLMChatHistoryTurn>
            {
                new LLMChatHistoryTurn("friend_b", "Sam", "Sam's last seen is 22:58.", false),
            };
            Assert.That(
                LLMChatReplyValidator.TryValidate(
                    "Sam's last seen is 22:58. Grey ticks. Not sure.",
                    "friend_a",
                    history,
                    out string reason),
                Is.False);
            Assert.That(reason, Is.EqualTo(LLMChatReplyValidator.ReasonCrossRepeat));
        }

        [Test]
        public void PlayerLine_Ignored()
        {
            var history = new List<LLMChatHistoryTurn>
            {
                new LLMChatHistoryTurn("player", "Player", Line, true),
            };
            Assert.That(
                LLMChatReplyValidator.TryValidate(Line, "friend_a", history, out _),
                Is.True);
        }

        [Test]
        public void ShortReplies_Allowed()
        {
            var history = new List<LLMChatHistoryTurn>
            {
                new LLMChatHistoryTurn("friend_b", "Sam", "yeah.", false),
            };
            Assert.That(
                LLMChatReplyValidator.TryValidate("yeah.", "friend_a", history, out _),
                Is.True);
        }

        [Test]
        public void OutsideLookback_Allowed()
        {
            var history = new List<LLMChatHistoryTurn>
            {
                new LLMChatHistoryTurn("friend_b", "Sam", Line, false),
            };
            for (int i = 0; i < 9; i++)
            {
                history.Add(new LLMChatHistoryTurn(
                    "friend_c", "Alex", "unrelated line number " + i + " here", false));
            }
            Assert.That(
                LLMChatReplyValidator.TryValidate(Line, "friend_a", history, out _),
                Is.True);
        }

        [Test]
        public void SelfRepeat_TakesPrecedence()
        {
            var history = new List<LLMChatHistoryTurn>
            {
                new LLMChatHistoryTurn("friend_b", "Sam", Line, false),
                new LLMChatHistoryTurn("friend_a", "Pat", Line, false),
            };
            Assert.That(
                LLMChatReplyValidator.TryValidate(Line, "friend_a", history, out string reason),
                Is.False);
            Assert.That(reason, Is.EqualTo(LLMChatReplyValidator.ReasonSelfRepeat));
        }

        [Test]
        public void DifferentLine_Passes()
        {
            var history = new List<LLMChatHistoryTurn>
            {
                new LLMChatHistoryTurn("friend_b", "Sam", Line, false),
            };
            Assert.That(
                LLMChatReplyValidator.TryValidate(
                    "nah I haven't checked the timestamps today",
                    "friend_a",
                    history,
                    out _),
                Is.True);
        }
    }
}
