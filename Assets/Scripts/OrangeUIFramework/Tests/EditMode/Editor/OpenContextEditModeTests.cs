using NUnit.Framework;

namespace Orange.UIFramework.Tests
{
    public sealed class OpenContextEditModeTests
    {
        [Test]
        public void GetPayload_ReturnsTypedPayload()
        {
            TestPayload payload = new TestPayload("shop");
            OpenContext context = new OpenContext(
                typeof(TestPageView),
                "page.shop",
                "ui_test",
                ViewKind.Page,
                payload,
                7);

            Assert.That(context.GetPayload<TestPayload>(), Is.SameAs(payload));
            Assert.That(context.GetPayload<OtherPayload>(), Is.Null);
            Assert.That(context.TryGetPayload(out TestPayload resolvedPayload), Is.True);
            Assert.That(resolvedPayload, Is.SameAs(payload));
            Assert.That(context.TryGetPayload(out OtherPayload missingPayload), Is.False);
            Assert.That(missingPayload, Is.Null);
        }

        private sealed class TestPayload
        {
            public TestPayload(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        private sealed class OtherPayload
        {
        }
    }
}
