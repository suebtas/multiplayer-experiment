using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using System.IO;
using System.Linq;
using Assets;

namespace CircuitRacerTests
{
    [TestFixture]
    public class RegExTests
    {
        [Test]
        [TestCase("/* Begin PBXBuildFile section */", "PBXBuildFile")]
        [TestCase("/* Begin PBXFileReference section */", "PBXFileReference")]
        [TestCase("/* End PBXBuildFile section */", null)]
        [TestCase("/* End PBXFileReference section */", null)]
        public void CanIdentifyBeginSection(string line, string name)
        {
            var regex = new Regex(@"\/\* Begin (?<name>[^ ]*) section \*\/");
            var match = regex.Match(line);
            var found = match.Success;

            Assert.That(found, Is.EqualTo(name != null));
            if(found)
                Assert.That(match.Groups["name"].Value, Is.EqualTo(name));
        }

        [Test]
        [TestCase("/* End PBXBuildFile section */", "PBXBuildFile")]
        [TestCase("/* End PBXFileReference section */", "PBXFileReference")]
        [TestCase("/* Begin PBXBuildFile section */", null)]
        [TestCase("/* Begin PBXFileReference section */", null)]
        public void CanIdentifyEndSection(string line, string name)
        {
            var regex = new Regex(@"\/\* End (?<name>[^ ]*) section \*\/");
            var match = regex.Match(line);
            var found = match.Success;

            Assert.That(found, Is.EqualTo(name != null));
            if(found)
                Assert.That(match.Groups["name"].Value, Is.EqualTo(name));
        }

        [Test]
        [TestCase("buildSettings = {", "buildSettings")]
        [TestCase("FRAMEWORK_SEARCH_PATHS = (", "FRAMEWORK_SEARCH_PATHS")]
        [TestCase("1D6058940D05DD3E006BFB54 /* Debug */ = {", "1D6058940D05DD3E006BFB54 /* Debug */")]
        [TestCase("1D6058940D05DD3E006BFB54 /* Debug */ = {something else", null)]
        public void CanIdentifySubSection(string line, string name)
        {
            var regex = new Regex(@"(?<name>[^=]*) = [\{\(]{1,1}$");
            var match = regex.Match(line);
            var found = match.Success;

            Assert.That(found, Is.EqualTo(name != null));
            if(found)
                Assert.That(match.Groups["name"].Value, Is.EqualTo(name));
        }

        [Test]
        public void CanMatchAllSubGroups()
        {
            var lines = File.ReadAllLines("../../files/Proj.txt");

            var regex = new Regex(@"(?<name>[^=]*) = [\{\(]{1,1}$");

            var stack = new Stack<string>();
            var found = 0;
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                var match = regex.Match(trimmedLine);
                if (match.Success)
                {
                    stack.Push(match.Groups["name"].Value);
                    ++found;
                    //Console.WriteLine("STACK:"+stack.JoinAsString("|"));
                }

                if (trimmedLine == ");" || trimmedLine == "};")
                {
                    stack.Pop();
                }
            }

            Console.WriteLine("STACKEND:"+stack.JoinAsString("|"));

            Assert.That(found, Is.GreaterThan(0));
            Assert.That(stack.Count, Is.EqualTo(0));
        }
    }
}