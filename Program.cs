using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using json = System.Text.Json.JsonSerializer;

namespace ReleaseRetention
{
    class Program
    {
        static void Main(string[] args)
        {
            Retainer r = Load();
            if (args.Length > 0)
            {
                string path = args[0];
                Console.Write("loading json files from {0}\n", Path.GetFullPath(path));
                r = Load(path);
            }
            else
            {
                Console.Write("using baked in test data\n");
            }
            Console.Write("Releases to retain\n");
            foreach (var retained in r.Retain(1))
            {
                Console.Write("  -> {0}\n", retained);
            }
        }

        // Load test data statically. 
        static Retainer Load()
        {
            // TODO: hardcode some test data. 
            return new Retainer
            {
            };
        }

        // Load test data from disk at the given path. 
        static Retainer Load(string dir)
        {
            return new Retainer
            {
                releases = json.Deserialize<List<Release>>(File.ReadAllText(Path.Join(dir, "Releases.json"))),
                projects = json.Deserialize<List<Project>>(File.ReadAllText(Path.Join(dir, "Projects.json"))),
                environments = json.Deserialize<List<Environment>>(File.ReadAllText(Path.Join(dir, "Environments.json"))),
                deployments = json.Deserialize<List<Deployment>>(File.ReadAllText(Path.Join(dir, "Deployments.json"))),
            };
        }
    }

    // Retainer implements logic for retaining releases. 
    class Retainer
    {
        public List<Project> projects;
        public List<Release> releases;
        public List<Environment> environments;
        public List<Deployment> deployments;

        // Retain the most recent n releases per project. 
        // Deployed releases are retained regardless.
        // Orphaned releases are pruned regardless, unless deployed.
        public List<RetainedRelease> Retain(int n)
        {
            var retained = new List<RetainedRelease>();
            // Note: Group by project id lets us handle the case where a release 
            // is deployed but the project has been deleted. 
            var groups = this.releases.GroupBy(
                r => r.ProjectId,
                r => r,
                (projectId, group) => new
                {
                    Project = this.projects.Where((Project p) => p.Id == projectId).FirstOrDefault(),
                    Releases = group.OrderByDescending((Release r) => r.Created),
                });
            foreach (var g in groups)
            {
                // Record up to n releases to be retained. 
                // Releases that are deployed are retained by default. 
                for (int ii = 0; ii < g.Releases.Count(); ii++)
                {
                    Release r = g.Releases.ElementAt(ii);
                    if (ii < n && !g.Project.Equals(default(Project)))
                    {
                        retained.Add(new RetainedRelease
                        {
                            project = g.Project,
                            release = r,
                            reason = String.Format("recency {0}/{1}", ii + 1, n),
                        });
                    }
                    else
                    {
                        bool isDeployed = this.deployments.Any((Deployment d) => d.ReleaseId == r.Id);
                        if (isDeployed)
                        {
                            Deployment d = this.deployments.Where((Deployment d) => d.ReleaseId == r.Id).First();
                            Environment env = this.environments.Find((Environment env) => d.EnvironmentId == env.Id);
                            retained.Add(new RetainedRelease
                            {
                                project = g.Project,
                                release = r,
                                reason = String.Format("currently deployed to {0}", env.Name),
                            });
                        }
                    }
                }
            }
            return retained;
        }
    }

    // RetainedRelease records a release to be retained and why. 
    // Note: reason could be an enum if we wanted to programatically react to it.
    struct RetainedRelease
    {
        public Project project { get; set; }
        public Release release { get; set; }
        public string reason { get; set; }

        public override string ToString()
        {
            return String.Format("{0}: {1} ({2}): {3}",
                this.project.Name != null ? this.project.Name : this.release.ProjectId,
                this.release.Id,
                this.release.Version != null ? "v" + this.release.Version : "unversioned",
                this.reason);
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
        public string DeployedAt { get; set; }
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