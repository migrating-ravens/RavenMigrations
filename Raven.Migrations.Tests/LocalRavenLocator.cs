using Raven.TestDriver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Raven.Migrations.Tests
{
    public class LocalRavenLocator : RavenServerLocator
    {
        private string _serverPath;
        private string _command = "dotnet";
        private readonly string RavenServerName = "Raven.Server";
        private string _arguments;

        public override string ServerPath
        {
            get
            {
                if (string.IsNullOrEmpty(_serverPath) == false)
                {
                    return _serverPath;
                }
                var path = Environment.GetEnvironmentVariable("Raven_Server_Test_Path");
                if (string.IsNullOrEmpty(path) == false)
                {
                    if (InitializeFromPath(path))
                        return _serverPath;
                }
                //If we got here we didn't have ENV:RavenServerTestPath setup for us maybe this is a CI environment
                path = Environment.GetEnvironmentVariable("Raven_Server_CI_Path");
                if (string.IsNullOrEmpty(path) == false)
                {
                    if (InitializeFromPath(path))
                        return _serverPath;
                }
                //We couldn't find Raven.Server in either enviroment variables lets look for it in the current directory
                foreach (var file in Directory.GetFiles(Environment.CurrentDirectory, $"{RavenServerName}.exe; {RavenServerName}.dll"))
                {
                    if (InitializeFromPath(file))
                        return _serverPath;
                }
                //Lets try some brute force
                foreach (var file in Directory.GetFiles(Directory.GetDirectoryRoot(Environment.CurrentDirectory), $"{RavenServerName}.exe; {RavenServerName}.dll", SearchOption.AllDirectories))
                {
                    if (InitializeFromPath(file))
                    {
                        try
                        {
                            //We don't want to override the variable if defined
                            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Raven_Server_Test_Path")))
                                Environment.SetEnvironmentVariable("Raven_Server_Test_Path", file);
                        }
                        //We might not have permissions to set the enviroment variable
                        catch
                        {

                        }
                        return _serverPath;
                    }
                }
                throw new FileNotFoundException($"Could not find {RavenServerName} anywhere on the device.");
            }
        }

        private bool InitializeFromPath(string path)
        {
            if (Path.GetFileNameWithoutExtension(path) != RavenServerName)
                return false;
            var ext = Path.GetExtension(path);
            if (ext == ".dll")
            {
                _serverPath = path;
                _arguments = _serverPath;
                return true;
            }
            if (ext == ".exe")
            {
                _serverPath = path;
                _command = _serverPath;
                _arguments = string.Empty;
                return true;
            }
            return false;
        }

        public override string Command => _command;
        public override string CommandArguments => _arguments;
    }
}
