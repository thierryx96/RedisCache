using System.Linq;
using NUnit.Framework;
using PEL.Framework.Redis.Extensions;

namespace PEL.Framework.Redis.IntegrationTests.Core
{
    [TestFixture]
    internal class Class1
    {
        [Test]
        public void ToUnitOrEmpty_WhenNull_ReturnsEmpty()
        {
            string key = null;

            var array = key.ToUnitOrEmpty();

            Assert.That(array, Is.Empty);
        }

        [Test]
        public void ToUnitOrEmpty_WhenValue_ReturnsEmpty()
        {
            var key = "chop";

            var single = key.ToUnitOrEmpty().Single();

            Assert.That(single, Is.EqualTo(key));
        }
    }
}