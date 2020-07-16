using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shard.Scripting;
using System.Collections.Generic;

namespace Shard.Tests.Scripting
{
    [TestClass]
    public class ScriptHostTest
    {
        [TestMethod("Set context value")]
        public void SetValue()
        {
            var context = new Dictionary<string, string>();
            var ret = ScriptHost.Evaluate("outputFilename = 'test'", context);
            Assert.AreEqual(ret, "test");
            Assert.IsTrue(context.TryGetValue("outputFilename", out var v) && v == "test");
        }

        [TestMethod("Replace context value")]
        public void ReplaceValue()
        {
            var context = new Dictionary<string, string> { { "outputFilename", "test_SVERIGE.xml" } };
            var ret = ScriptHost.Evaluate("outputFilename = outputFilename.replace('_SVERIGE', '')", context);
            Assert.AreEqual(ret, "test.xml");
            Assert.IsTrue(context.TryGetValue("outputFilename", out var v) && v == "test.xml");
        }

        [TestMethod("Set multiple")]
        public void SetMultiple()
        {
            var context = new Dictionary<string, string>();
            var ret = ScriptHost.Evaluate("foo = 1, bar = 2", context);
            Assert.AreEqual(ret, "2");
            Assert.IsTrue(context.TryGetValue("foo", out var foo) && foo == "1");
            Assert.IsTrue(context.TryGetValue("bar", out var bar) && bar == "2");
        }

        [TestMethod("Set multiple destructuring")]
        public void SetMultipleDestr()
        {
            var context = new Dictionary<string, string>();
            var ret = ScriptHost.Evaluate("[foo, bar] = [1, 2]", context);
            Assert.AreEqual(ret, null);
            Assert.IsTrue(context.TryGetValue("foo", out var foo) && foo == "1");
            Assert.IsTrue(context.TryGetValue("bar", out var bar) && bar == "2");
        }
    }
}