using System.Collections.Generic;
using NUnit.Framework;

namespace AmberScreen.LLMChat.Tests
{
    public sealed class ReplyValidatorTests
    {
        [Test]
        public void SelfRepeat_Rejected_NewLine_Accepted()
        {
            var history = new List<LLMChatHistoryTurn>
            {
                new LLMChatHistoryTurn("friend_a", "Alex", "who hurt u again. they've been busy.", false),
                new LLMChatHistoryTurn("player", "Player", "hey", true),
                new LLMChatHistoryTurn("friend_a", "Alex", "who hurt u again. they've been busy. na hh h.", false),
            };

            Assert.That(
                LLMChatReplyValidator.TryValidate(
                    "who hurt u again. they've been busy.",
                    "friend_a",
                    history,
                    out string reason),
                Is.False);
            Assert.That(reason, Is.EqualTo(LLMChatReplyValidator.ReasonSelfRepeat));

            Assert.That(
                LLMChatReplyValidator.TryValidate(
                    "nah mate i haven't heard from him today",
                    "friend_a",
                    history,
                    out string okReason),
                Is.True);
            Assert.That(okReason, Is.Null);
        }

        [Test]
        public void Similarity_IdenticalNormalised_IsHigh()
        {
            float sim = LLMChatReplyValidator.Similarity(
                LLMChatReplyValidator.Normalize("Who hurt u 💀 again!!!"),
                LLMChatReplyValidator.Normalize("who hurt u again"));
            Assert.That(sim, Is.GreaterThanOrEqualTo(LLMChatReplyValidator.SelfRepeatSimilarityThreshold));
        }
    }
}
