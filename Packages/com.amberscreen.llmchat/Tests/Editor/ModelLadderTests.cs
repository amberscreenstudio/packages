using NUnit.Framework;

namespace AmberScreen.LLMChat.Tests
{
    public sealed class ModelLadderTests
    {
        [Test]
        public void WarmupUnderBudget_StaysOnPreferred()
        {
            var ladder = new ModelLadder(new[] { "A.gguf", "B.gguf", "C.gguf" }, 10f);
            Assert.That(ladder.EvaluateWarmup(4500, out _, out _), Is.False);
            Assert.That(ladder.ActiveModel, Is.EqualTo("A.gguf"));
        }

        [Test]
        public void WarmupOverBudget_StepsDown_FakeTimer()
        {
            var ladder = new ModelLadder(new[] { "A.gguf", "B.gguf", "C.gguf" }, 10f);
            Assert.That(ladder.EvaluateWarmup(15000, out string from, out string to), Is.True);
            Assert.That(from, Is.EqualTo("A.gguf"));
            Assert.That(to, Is.EqualTo("B.gguf"));
            Assert.That(ladder.ActiveModel, Is.EqualTo("B.gguf"));
        }

        [Test]
        public void MedianOfFirstThreeReplies_OverBudget_StepsDown()
        {
            var ladder = new ModelLadder(new[] { "A.gguf", "B.gguf" }, 10f);
            Assert.That(ladder.EvaluateReply(11000, out _), Is.False);
            Assert.That(ladder.EvaluateReply(12000, out _), Is.False);
            Assert.That(ladder.EvaluateReply(9000, out long? median), Is.True);
            Assert.That(median, Is.EqualTo(11000L));
            Assert.That(ladder.TryStepDown(out string from, out string to), Is.True);
            Assert.That(from, Is.EqualTo("A.gguf"));
            Assert.That(to, Is.EqualTo("B.gguf"));
            Assert.That(ladder.ActiveModel, Is.EqualTo("B.gguf"));
        }

        [Test]
        public void MedianUnderBudget_DoesNotStepDown()
        {
            var ladder = new ModelLadder(new[] { "A.gguf", "B.gguf" }, 10f);
            Assert.That(ladder.EvaluateReply(4000, out _), Is.False);
            Assert.That(ladder.EvaluateReply(5000, out _), Is.False);
            Assert.That(ladder.EvaluateReply(6000, out long? median), Is.False);
            Assert.That(median, Is.EqualTo(5000L));
            Assert.That(ladder.ActiveModel, Is.EqualTo("A.gguf"));
        }

        [Test]
        public void LastResort_CannotStepDown()
        {
            var ladder = new ModelLadder(new[] { "only.gguf" }, 1f);
            Assert.That(ladder.EvaluateWarmup(99999, out _, out _), Is.False);
            Assert.That(ladder.ActiveModel, Is.EqualTo("only.gguf"));
        }

        [Test]
        public void Median_EvenCount_AveragesMiddlePair()
        {
            Assert.That(ModelLadder.Median(new long[] { 10, 20, 30, 40 }), Is.EqualTo(25L));
        }
    }
}
