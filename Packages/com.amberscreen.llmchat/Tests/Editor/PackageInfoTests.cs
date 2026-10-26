using NUnit.Framework;
using UnityEditor.PackageManager;

namespace AmberScreen.LLMChat.Tests
{
    public sealed class PackageInfoTests
    {
        [Test]
        public void PackageInfo_NameVersionDependencies()
        {
            var info = PackageInfo.FindForAssembly(typeof(LLMChatRunner).Assembly);
            Assert.That(info, Is.Not.Null);
            Assert.That(info.name, Is.EqualTo("com.amberscreen.llmchat"));
            Assert.That(info.version, Is.EqualTo("0.2.0"));
            Assert.That(info.dependencies == null || info.dependencies.Length == 0, Is.True);
        }
    }
}
