using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System;
using System.Collections.Generic;

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
                .Where(o => !r.Deployments.Any(d => d.ReleaseId == o.Id));

            Assert.AreEqual(
                r.Retain(r.Releases.Count()).Count(),
                r.Releases.Count() - undeployedOrphans.Count()
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

            var retainedOrphans = r.Retain(r.Releases.Count())
                .Where(retained => r.Orphans().Contains(retained.Release));

            var deployedOrphans = r.Orphans()
                .Where(o => r.Deployments.Any(d => d.ReleaseId == o.Id));

            Assert.AreEqual(deployedOrphans.Count(), retainedOrphans.Count());
            Assert.IsFalse(retainedOrphans.Any(o => !r.Deployments.Any(d => d.ReleaseId == o.Release.Id)));
        }

        [TestMethod]
        public void TestRetainDeployments()
        {
            var tests = new[] {
                new {
                    Label = "empty data should return an empty list",
                    N = 0,
                    Data = new Retainer {},
                    Want = new List<RetainedDeployment>{}
                },
                new {
                    Label = "empty data should return an empty list, regardless of n",
                    N = 100,
                    Data = new Retainer {},
                    Want = new List<RetainedDeployment>{}
                },
                new {
                    Label = "should retain no deployments with n of zero",
                    N = 0,
                    Data = new Retainer {
                        Projects = new List<Project>(){
                            new Project {
                                Id = "project-1",
                                Name = "project-1",
                            },
                            new Project {
                                Id = "project-2",
                                Name = "project-2",
                            },
                        },
                        Releases = new List<Release>(){
                            new Release {
                                Id = "relase-1",
                                ProjectId = "project-1",
                                Version = "",
                                Created = new DateTime(),
                            },
                        },
                        Deployments = new List<Deployment>(){
                            new Deployment {
                                Id = "deployment-1",
                                ReleaseId = "release-1",
                                EnvironmentId = "staging",
                                DeployedAt = new DateTime(),
                            },
                        },
                        Environments = new List<Environment>(){
                            new Environment{
                                Id = "staging",
                                Name = "staging",
                            }
                        },
                    },
                    Want = new List<RetainedDeployment>{}
                },
                new {
                    Label = "should retain the single most recent deployment per project-environment",
                    N = 1,
                    Data = new Retainer {
                        Projects = new List<Project>(){
                            new Project {
                                Id = "project-1",
                            },
                        },
                        Releases = new List<Release>(){
                            new Release {
                                Id = "release-1",
                                ProjectId = "project-1",
                                Version = "",
                                Created = new DateTime(),
                            },
                        },
                        Deployments = new List<Deployment>(){
                            new Deployment {
                                Id = "deployment-1",
                                ReleaseId = "release-1",
                                EnvironmentId = "staging",
                                DeployedAt = new DateTime(),
                            },
                        },
                        Environments = new List<Environment>(){
                            new Environment{
                                Id = "staging",
                            }
                        },
                    },
                    Want = new List<RetainedDeployment>(){
                        new RetainedDeployment{
                            DeploymentId = "deployment-1",
                            ProjectId = "project-1",
                            EnvironmentId = "staging",
                            ReleaseId = "release-1"
                        },
                    }
                },
                new {
                    Label = "should retain the single most recent deployment per project-environment",
                    N = 1,
                    Data = new Retainer {
                        Projects = new List<Project>(){
                            new Project {
                                Id = "project-1",
                            },
                            new Project {
                                Id = "project-2",
                            },
                        },
                        Releases = new List<Release>(){
                            new Release {
                                Id = "release-1",
                                ProjectId = "project-1",
                                Version = "",
                                Created = new DateTime(),
                            },
                            new Release {
                                Id = "release-2",
                                ProjectId = "project-1",
                                Version = "",
                                Created = new DateTime(),
                            },
                            new Release {
                                Id = "release-3",
                                ProjectId = "project-2",
                                Version = "",
                                Created = new DateTime(),
                            },
                            new Release {
                                Id = "release-4",
                                ProjectId = "project-2",
                                Version = "",
                                Created = new DateTime(),
                            },
                        },
                        Deployments = new List<Deployment>(){
                            new Deployment {
                                Id = "deployment-1",
                                ReleaseId = "release-1",
                                EnvironmentId = "staging",
                                DeployedAt = new DateTime(),
                            },
                            new Deployment {
                                Id = "deployment-2",
                                ReleaseId = "release-2",
                                EnvironmentId = "production",
                                DeployedAt = new DateTime(),
                            },
                            new Deployment {
                                Id = "deployment-3",
                                ReleaseId = "release-3",
                                EnvironmentId = "staging",
                                DeployedAt = new DateTime(),
                            },
                            new Deployment {
                                Id = "deployment-4",
                                ReleaseId = "release-4",
                                EnvironmentId = "production",
                                DeployedAt = DateTime.MinValue,
                            },
                            new Deployment {
                                Id = "deployment-5",
                                ReleaseId = "release-4",
                                EnvironmentId = "production",
                                DeployedAt = DateTime.MaxValue,
                            },
                        },
                        Environments = new List<Environment>(){
                            new Environment{
                                Id = "staging",
                            },
                            new Environment{
                                Id = "production",
                            }
                        },
                    },
                    Want = new List<RetainedDeployment>(){
                        new RetainedDeployment{
                            ProjectId = "project-1",
                            ReleaseId = "release-1",
                            DeploymentId = "deployment-1",
                            EnvironmentId = "staging",
                        },
                        new RetainedDeployment{
                            ProjectId = "project-1",
                            ReleaseId = "release-2",
                            DeploymentId = "deployment-2",
                            EnvironmentId = "production",
                        },
                        new RetainedDeployment{
                            ProjectId = "project-2",
                            ReleaseId = "release-3",
                            DeploymentId = "deployment-3",
                            EnvironmentId = "staging",
                        },
                        new RetainedDeployment{
                            ProjectId = "project-2",
                            ReleaseId = "release-4",
                            DeploymentId = "deployment-5",
                            EnvironmentId = "production",
                        },
                    }
                },
                new {
                    Label = "retain up to two most recent deployments per project-environment",
                    N = 2,
                    Data = new Retainer {
                        Projects = new List<Project>(){
                            new Project {
                                Id = "project-1",
                            },
                            new Project {
                                Id = "project-2",
                            },
                        },
                        Releases = new List<Release>(){
                            new Release {
                                Id = "release-1",
                                ProjectId = "project-1",
                                Version = "",
                                Created = new DateTime(),
                            },
                            new Release {
                                Id = "release-2",
                                ProjectId = "project-1",
                                Version = "",
                                Created = new DateTime(),
                            },
                            new Release {
                                Id = "release-3",
                                ProjectId = "project-2",
                                Version = "",
                                Created = new DateTime(),
                            },
                            new Release {
                                Id = "release-4",
                                ProjectId = "project-2",
                                Version = "",
                                Created = new DateTime(),
                            },
                        },
                        Deployments = new List<Deployment>(){
                            new Deployment {
                                Id = "deployment-1",
                                ReleaseId = "release-1",
                                EnvironmentId = "staging",
                                DeployedAt = new DateTime(),
                            },
                            new Deployment {
                                Id = "deployment-2",
                                ReleaseId = "release-2",
                                EnvironmentId = "production",
                                DeployedAt = new DateTime(),
                            },
                            new Deployment {
                                Id = "deployment-3",
                                ReleaseId = "release-3",
                                EnvironmentId = "staging",
                                DeployedAt = new DateTime(),
                            },
                            new Deployment {
                                Id = "deployment-4",
                                ReleaseId = "release-4",
                                EnvironmentId = "production",
                                DeployedAt = DateTime.MinValue,
                            },
                            new Deployment {
                                Id = "deployment-5",
                                ReleaseId = "release-4",
                                EnvironmentId = "production",
                                DeployedAt = DateTime.MaxValue,
                            },
                        },
                        Environments = new List<Environment>(){
                            new Environment{
                                Id = "staging",
                            },
                            new Environment{
                                Id = "production",
                            }
                        },
                    },
                    Want = new List<RetainedDeployment>(){
                        new RetainedDeployment{
                            ProjectId = "project-1",
                            ReleaseId = "release-1",
                            DeploymentId = "deployment-1",
                            EnvironmentId = "staging",
                        },
                        new RetainedDeployment{
                            ProjectId = "project-1",
                            ReleaseId = "release-2",
                            DeploymentId = "deployment-2",
                            EnvironmentId = "production",
                        },
                        new RetainedDeployment{
                            ProjectId = "project-2",
                            ReleaseId = "release-3",
                            DeploymentId = "deployment-3",
                            EnvironmentId = "staging",
                        },
                        new RetainedDeployment{
                            ProjectId = "project-2",
                            ReleaseId = "release-4",
                            DeploymentId = "deployment-5",
                            EnvironmentId = "production",
                        },
                        new RetainedDeployment{
                            ProjectId = "project-2",
                            ReleaseId = "release-4",
                            DeploymentId = "deployment-4",
                            EnvironmentId = "production",
                        },
                    }
                },
            }.ToList();
            foreach (var test in tests)
            {
                var got = test.Data.RetainDeployments(test.N);
                Assert.IsTrue(
                    Enumerable.SequenceEqual(got, test.Want),
                    String.Format(
                        "{0}: \nwant \n\t{1}, \ngot \n\t{2}",
                        test.Label,
                        string.Join(",\n\t", test.Want),
                        string.Join(",\n\t", got)
                    ));
            }
        }
    }
}
