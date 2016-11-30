using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PEL.Framework.Redis.Extensions;

namespace PEL.Framework.Redis.IntegrationTests.Core
{
    [TestFixture]
    class Class1
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
            string key = "chop";

            var single = key.ToUnitOrEmpty().Single();

            Assert.That(single, Is.EqualTo(key));
        }
    }
}
