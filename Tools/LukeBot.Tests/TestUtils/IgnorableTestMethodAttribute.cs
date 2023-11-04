using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace LukeBot.Tests
{
    public class IgnorableTestMethodAtribute: TestMethodAttribute
    {
        protected virtual bool ShouldIgnore(ITestMethod testMethod)
        {
            return false;
        }

        public override TestResult[] Execute(ITestMethod testMethod)
        {
            if (ShouldIgnore(testMethod))
                return new TestResult[]
                {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Inconclusive
                    }
                };
            else
                return base.Execute(testMethod);
        }
    }
}