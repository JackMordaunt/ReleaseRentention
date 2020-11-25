using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;

namespace ReleaseRetention
{
    [TestClass]
    public class TestRetainer
    {

        // Load data from disk for convenience.
        Retainer r = Retainer.Load("../../../data");

        [TestMethod]
        public void TestRetainReleases()
        {
            // A release is retained when it's either deployed or recent. 
            // If deployed, that includes orphans.
            // If recent, that excludes orphans. 
            //
            // Therefore retained.Count = releases.Count - undeployedOrphans.Count

            var undeployedOrphans = r.Orphans()
                .Where(o => !r.deployments.Any(d => d.ReleaseId == o.Id));

            Assert.AreEqual(
                r.Retain(r.releases.Count()).Count(),
                r.releases.Count() - undeployedOrphans.Count()
            );
        }

        [TestMethod]
        public void TestRetainDeployedReleases()
        {
            // At least deployed releases must be retained.
            Assert.AreEqual(
                r.Deployed().Count(),
                r.Retain(0).Count()
            );
        }

        [TestMethod]
        public void TestIgnoreUndeployedOrphans()
        {
            // Orphaned releases that are not deployed are not retained. 

            var retainedOrphans = r.Retain(r.releases.Count())
                .Where(retained => r.Orphans().Contains(retained.release));

            var deployedOrphans = r.Orphans()
                .Where(o => r.deployments.Any(d => d.ReleaseId == o.Id));

            Assert.AreEqual(deployedOrphans.Count(), retainedOrphans.Count());
            Assert.IsFalse(retainedOrphans.Any(o => !r.deployments.Any(d => d.ReleaseId == o.release.Id)));
        }
    }
}
