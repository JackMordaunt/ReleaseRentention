using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using json = System.Text.Json.JsonSerializer;

namespace ReleaseRetention
{
    // Retainer implements logic for retaining releases. 
    class Retainer
    {
        public List<Project> Projects = new List<Project>();
        public List<Release> Releases = new List<Release>();
        public List<Environment> Environments = new List<Environment>();
        public List<Deployment> Deployments = new List<Deployment>();

        // Retain the most recent n releases per project. 
        // Deployed releases are retained regardless.
        // Orphaned releases are ignored regardless, unless deployed.
        public List<RetainedRelease> Retain(int n)
        {
            var retained = new List<RetainedRelease>();
            // Note: Group by project id lets us handle the case where a release 
            // is deployed but the project has been deleted. 
            var groups = this.Releases.GroupBy(
                r => r.ProjectId,
                r => r,
                (projectId, group) => new
                {
                    Project = this.Projects.Where((Project p) => p.Id == projectId).FirstOrDefault(),
                    Releases = group.OrderByDescending((Release r) => r.Created),
                });
            foreach (var g in groups)
            {
                // Record up to n releases to be retained. 
                for (int ii = 0; ii < g.Releases.Count(); ii++)
                {
                    Release r = g.Releases.ElementAt(ii);
                    if (ii < n && !g.Project.Equals(default(Project)))
                    {
                        retained.Add(new RetainedRelease
                        {
                            Project = g.Project,
                            Release = r,
                            Reason = String.Format("recency {0}/{1}", ii + 1, n),
                        });
                    }
                    else if (this.IsDeployed(r.Id))
                    {
                        Deployment d = this.Deployments.Find((Deployment d) => d.ReleaseId == r.Id);
                        Environment env = this.Environments.Find((Environment env) => d.EnvironmentId == env.Id);
                        retained.Add(new RetainedRelease
                        {
                            Project = g.Project,
                            Release = r,
                            Reason = String.Format("currently deployed to {0}", env.Name),
                        });
                    }
                }
            }
            return retained;
        }

        // RetainDeploymet retains the most recent `n` deployments for a given 
        // project-environment pair. 
        public List<RetainedDeployment> RetainDeployments(int n)
        {
            List<RetainedDeployment> retained = new List<RetainedDeployment>();
            // Iterate deployment-wise and pull all the data together.
            // Take up to n number deployments ordered by most recent first.
            var groups = this.Deployments.GroupBy(
                d => new
                {
                    Project = this.Projects.Find(p => p.Id == this.Releases.Find(r => r.Id == d.ReleaseId).ProjectId).Id,
                    Environment = this.Environments.Find(e => e.Id == d.EnvironmentId).Id,
                },
                d => new
                {
                    Deployment = d,
                    Project = this.Projects.Find(p => p.Id == this.Releases.Find(r => r.Id == d.ReleaseId).ProjectId),
                    Environment = this.Environments.Find(e => e.Id == d.EnvironmentId),
                },
                (d, group) => group.OrderByDescending(d => d.Deployment.DeployedAt).Take(n)
            );
            // Flatten data into list. 
            foreach (var group in groups)
            {
                foreach (var d in group)
                {
                    var retain = new RetainedDeployment
                    {
                        DeploymentId = d.Deployment.Id,
                        ProjectId = d.Project.Id,
                        EnvironmentId = d.Environment.Id,
                        ReleaseId = d.Deployment.ReleaseId,
                    };
                    retained.Add(retain);
                }
            }
            return retained;
        }

        // Orphans returns a list of orphaned releases.
        // An orphaned release is a release with no active project.
        public IEnumerable<Release> Orphans()
        {
            return this.Releases.Where(r => !this.Projects.Any(p => p.Id == r.ProjectId));
        }

        // Deployed returns a list of deployed releases.
        public IEnumerable<Release> Deployed()
        {
            return this.Releases.Where(r => this.Deployments.Any(d => d.ReleaseId == r.Id));
        }

        // IsDeployed if the given release has an associated deployment. 
        public bool IsDeployed(string ReleaseId)
        {
            return this.Deployments.Any((Deployment d) => d.ReleaseId == ReleaseId);
        }

        // Load test data from disk at the given path. 
        public static Retainer Load(string dir)
        {
            return new Retainer
            {
                Releases = json.Deserialize<List<Release>>(File.ReadAllText(Path.Join(dir, "Releases.json"))),
                Projects = json.Deserialize<List<Project>>(File.ReadAllText(Path.Join(dir, "Projects.json"))),
                Environments = json.Deserialize<List<Environment>>(File.ReadAllText(Path.Join(dir, "Environments.json"))),
                Deployments = json.Deserialize<List<Deployment>>(File.ReadAllText(Path.Join(dir, "Deployments.json"))),
            };
        }
    }

    // RetainedRelease records a release to be retained and why. 
    // Note: reason could be an enum if we wanted to programatically react to it.
    struct RetainedRelease
    {
        public Project Project { get; set; }
        public Release Release { get; set; }
        public string Reason { get; set; }

        public override string ToString()
        {
            return String.Format("{0}: {1} ({2}): {3}",
                this.Project.Name != null ? this.Project.Name : this.Release.ProjectId,
                this.Release.Id,
                this.Release.Version != null ? "v" + this.Release.Version : "unversioned",
                this.Reason);
        }
    }

    // RetaiendDeploymet records a deployment to be retained and why. 
    struct RetainedDeployment
    {
        public string ReleaseId { get; set; }
        public string ProjectId { get; set; }
        public string EnvironmentId { get; set; }
        public string DeploymentId { get; set; }
        // public string Reason { get; set; }

        public override string ToString()
        {
            return String.Format(
                "(Deployment = {0}, Project = {1}, Environment = {2}, Release = {3})",
                this.DeploymentId,
                this.ProjectId,
                this.EnvironmentId,
                this.ReleaseId);
        }
    }

    struct Release
    {
        public string Id { get; set; }
        public string ProjectId { get; set; }
        public string Version { get; set; }
        public DateTime Created { get; set; }
    }

    struct Deployment
    {
        public string Id { get; set; }
        public string ReleaseId { get; set; }
        public string EnvironmentId { get; set; }
        public DateTime DeployedAt { get; set; }
    }

    struct Environment
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    struct Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

}