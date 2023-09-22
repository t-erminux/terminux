using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using Renci.SshNet;



class Terminux
{
    private static SshClient sshClient;
    static void Main()
    {

        if (!IsAdministrator())
        {
            Console.Title = "Terminux v1.0.0";
            Console.WriteLine("Please run the terminal as an administrator.");
            Console.ReadLine();
            return;
        }
        else
            Console.Write("(c) Copyright Terminux 2023\r\nMicrosoft Corporation. All rights reserved.\r\n\r\n");

        bool isAdmin = false;
        string clipboard = string.Empty;


        string user = Environment.UserName;

        // Get the desktop name (you may need to modify this part)
        string desktopName = Environment.MachineName;

        while (true)
        {
            Console.Title = "Terminux v1.0.0";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{user}@");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{desktopName} ");

            // Reset text color for the command input and pointer
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.Write("$~ ");

            // Check if Ctrl key is pressed for copy and paste functionality
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
                {
                    if (keyInfo.Key == ConsoleKey.C)
                    {
                        clipboard = Console.ReadLine();
                        continue;
                    }
                    else if (keyInfo.Key == ConsoleKey.V)
                    {
                        Console.Write(clipboard);
                        continue;
                    }
                }
            }

            string input = Console.ReadLine();
            string[] commandArgs = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (commandArgs.Length > 0)
            {
                string command = commandArgs[0].ToLower();

                switch (command)
                {
                    case "runadmin":
                        isAdmin = true;
                        break;
                    case "cd":
                        if (commandArgs.Length > 1)
                        {
                            try
                            {
                                Directory.SetCurrentDirectory(commandArgs[1]);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error: {ex.Message}");
                            }
                        }
                        break;
                    case "write":
                        if (commandArgs.Length > 1)
                        {
                            Console.WriteLine(string.Join(" ", commandArgs.Skip(1)));
                        }
                        break;
                    case "upt":
                        if (commandArgs.Length > 2 && commandArgs[1] == "install")
                        {
                            string downloadLink = commandArgs[2];
                            InstallApp(downloadLink);
                        }
                        else
                        {
                            Console.WriteLine("Invalid 'upt' command format. Use 'upt install downloadLink' to install an app.");
                        }
                        break;

                    case "help":
                        ShowHelp();
                        break;

                    case "neofetch":
                        ExecuteNeofetch();
                        break;
                    case "whoami":
                        whoami();
                        break;
                    case "winversion":
                        winversion();
                        break;

                    case "uwpman":
                        UwpManagerCommand(commandArgs);
                        break;

                    case "exit":
                        Environment.Exit(0);
                        break;
                    case "uwpman -list":
                        ListUWPApps();
                        break;
                    case "dir":
                        ListDirectoryContents(Environment.CurrentDirectory);
                        break;
                    case "clr":
                        clear();
                        break;
                    case "clear":
                        clear();
                        break;
                    case "about":
                        about();
                        break;

                    case "run":
                        if (commandArgs.Length > 1)
                        {
                            string filePath = commandArgs[1];
                            RunFile(filePath);
                        }
                        else
                        {
                            Console.WriteLine("Usage: run [file path]");
                        }
                        break;


                    default:
                        Console.WriteLine("Invalid command, \r\nMay be due to the file unrecognized.");
                        break;



                    case "uptrun":
                        if (commandArgs.Length > 1)
                        {
                            string appName = commandArgs[1];
                            ExecuteUptRun(appName);
                        }
                        else
                        {
                            Console.WriteLine("Invalid 'uptrun' command format. Use 'uptrun \"app name\"' to run an installed app.");
                        }
                        break;

                    case "git":
                        if (commandArgs.Length > 1)
                        {
                            string gitCommand = string.Join(" ", commandArgs.Skip(1));

                            ExecuteGitCommand(gitCommand);
                        }
                        else
                        {
                            Console.WriteLine("Invalid 'git' command format. Use 'git [git-command]' to run a Git command.");
                        }
                        break;

                    case "isgitinstalled":
                        if (IsGitInstalled())
                        {
                            Console.WriteLine("Git is installed on this system.");
                        }
                        else
                        {
                            Console.WriteLine("Git is not installed on this system.");
                        }
                        break;

                    case "sshconnect":
                        if (commandArgs.Length == 2)
                        {
                            string sshCommand = commandArgs[1];
                            if (sshCommand.StartsWith("ssh "))
                            {
                                sshCommand = sshCommand.Substring(4); // Remove "ssh " prefix
                                string[] sshArgs = sshCommand.Split('@');

                                if (sshArgs.Length == 2)
                                {
                                    string username = sshArgs[0];
                                    string host = sshArgs[1];

                                    // You can also prompt for the SSH password here if needed
                                    Console.Write("Enter SSH password: ");
                                    string password = Console.ReadLine();

                                    sshClient = EstablishSSHConnection(host, username, password);
                                    Console.WriteLine("SSH connection established.");
                                }
                                else
                                {
                                    Console.WriteLine("Invalid SSH command. Use 'ssh user@host' to initiate an SSH connection.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid SSH command. Use 'ssh user@host' to initiate an SSH connection.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid 'sshconnect' command format. Use 'sshconnect ssh user@host'.");
                        }
                        break;


                    case "sshcommand":
                        if (sshClient != null)
                        {
                            Console.WriteLine("SSH connection is already established.");
                        }
                        else
                        {
                            Console.Write("Enter SSH server hostname or IP address: ");
                            string host = Console.ReadLine();

                            Console.Write("Enter SSH username: ");
                            string username = Console.ReadLine();

                            Console.Write("Enter SSH password: ");
                            string password = GetPassword(); // Use a method to securely read the password

                            string sshCommand = $"ssh {username}@{host}";

                            ProcessStartInfo psi = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/C {sshCommand}",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = false // Set this to true if you want a console window
                            };

                            using (Process process = new Process())
                            {
                                process.StartInfo = psi;
                                process.Start();
                            }
                        }
                        break;

                    case "sshdisconnect":
                        if (sshClient != null)
                        {
                            sshClient.Disconnect();
                            sshClient.Dispose();
                            sshClient = null;
                            Console.WriteLine("SSH connection closed.");
                        }
                        else
                        {
                            Console.WriteLine("SSH connection is not established.");
                        }
                        break;

                    case "cmatrix":
                        break;


                }
            }

            else
                Console.WriteLine("Multiple Lines Detected.");
        }
    }


    static void ShowHelp()
    {
        Console.WriteLine("Coming Soon, Who knows?");
    }

    static void UninstallUWPApp(string[] appNames)
    {
        foreach (string appName in appNames)
        {
            Process.Start("powershell.exe", $"Remove-AppxPackage {appName}");

            Console.WriteLine($"Uninstalling {appName}...");
        }
    }

    static void ListUWPApps()
    {
        try
        {
            // Create a PowerShell script to list UWP apps.
            string powerShellScript = "Get-AppxPackage | Select-Object Name, PackageFullName";

            // Create a PowerShell process.
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"-NoProfile -ExecutionPolicy unrestricted -Command \"{powerShellScript}\""
            };

            using (Process process = new Process())
            {
                process.StartInfo = psi;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Error: {error}");
                }
                else
                {
                    Console.WriteLine("List of UWP Apps:");
                    Console.WriteLine(output);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }


    static bool IsAdministrator()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    static void whoami()
    {
        string user = Environment.UserName;
        Console.WriteLine("Your name is ");
        Console.WriteLine(user);
    }

    private static string GetPassword()
    {
        StringBuilder password = new StringBuilder();
        ConsoleKeyInfo keyInfo;

        do
        {
            keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                // Remove the last character from the password if Backspace is pressed.
                password.Remove(password.Length - 1, 1);
                Console.Write("\b \b"); // Clear the character from the console.
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                // Append the character to the password and display an asterisk.
                password.Append(keyInfo.KeyChar);
                Console.Write("*");
            }
        } while (keyInfo.Key != ConsoleKey.Enter);

        Console.WriteLine(); // Move to the next line after the user presses Enter.

        return password.ToString();
    }

    static void ExecuteNeofetch()
    {
        try
        {
            string installsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "installs");
            string neofetchPath = Path.Combine(installsDirectory, "neofetch.exe");

            if (File.Exists(neofetchPath))
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = neofetchPath,
                    UseShellExecute = false, // This prevents a new window
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();

                    // Read and print the output (if needed)
                    string output = process.StandardOutput.ReadToEnd();
                    Console.WriteLine(output);

                    process.WaitForExit();
                }
            }
            else
            {
                Console.WriteLine("Neofetch not found in the 'installs' folder.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    static void InstallApp(string downloadLink)
    {
        try
        {
            // Create a directory for installations if it doesn't exist
            string installDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "installs");
            Directory.CreateDirectory(installDirectory);

            // Create a temporary file to store the downloaded zip
            string tempZipFile = Path.Combine(installDirectory, "temp.zip");

            // Download the zip file
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(downloadLink, tempZipFile);
                Console.WriteLine($"Downloaded {downloadLink}");
            }

            // Extract the zip file to the installs folder
            ZipFile.ExtractToDirectory(tempZipFile, installDirectory);
            Console.WriteLine($"Installed in {installDirectory}");

            // Delete the temporary zip file
            File.Delete(tempZipFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void winversion()
    {
        Process.Start("winver.exe");
    }

    static void RunFile(string filePath)
    {
        try
        {
            Process.Start(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }




    static void clear()
    {
        Console.Clear();

    }

    static SshClient EstablishSSHConnection(string host, string username, string password)
    {
        ConnectionInfo connectionInfo = new ConnectionInfo(host, username, new PasswordAuthenticationMethod(username, password));
        SshClient sshClient = new SshClient(connectionInfo);
        sshClient.Connect();
        return sshClient;
    }

    static void ExecuteSSHCommand(SshClient sshClient, string command)
    {
        SshCommand sshCommand = sshClient.CreateCommand(command);
        sshCommand.Execute();
        Console.WriteLine(sshCommand.Result);
    }




    static void about()
    {
        Console.WriteLine("Terminux, Terminux Software LLC.\r\n\r\nVersion: 1.0\r\nStage: Alpha\r\nBuild: 5829.287", "About Terminux");
    }

    static void netopti()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "ipconfig /renew";
        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;
        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();
        process.Dispose();



    }

    static void UwpManagerCommand(string[] commandArgs)
    {
        if (commandArgs.Length > 1)
        {
            switch (commandArgs[1].ToLower())
            {
                case "uninstall":
                    if (commandArgs.Length > 2)
                    {
                        UninstallUWPApp(commandArgs.Skip(2).ToArray());
                    }
                    else
                    {
                        Console.WriteLine("Please provide at least one UWP app name to uninstall.");
                    }
                    break;
                case "list":
                    if (commandArgs.Length > 2 && commandArgs[2].ToLower() == "apps")
                    {
                        ListUWPApps();
                    }
                    else
                    {
                        Console.WriteLine("Invalid command format. Use 'uwpman list apps' to list all UWP apps.");
                    }
                    break;
                default:
                    Console.WriteLine("Invalid subcommand.");
                    break;
            }
        }
        else
        {
            Console.WriteLine("Invalid uwpman command.");
        }
    }



    static void ExecuteGitCommand(string gitCommand)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = gitCommand,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = psi;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Git Error: {error}");
                }
                else
                {
                    Console.WriteLine(output);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void ExecuteUptRun(string appName)
    {
        try
        {
            string installsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "installs");
            string executablePath = Path.Combine(installsDirectory, $"{appName}.exe");

            if (File.Exists(executablePath))
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();

                    // Read and print the output (if needed)
                    string output = process.StandardOutput.ReadToEnd();
                    Console.WriteLine(output);

                    process.WaitForExit();
                }
            }
            else
            {
                Console.WriteLine($"uptrun can't find the executable file '{appName}' - error code 0");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }



    static bool IsGitInstalled()
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = psi;
                process.Start();
                process.WaitForExit();

                // If Git is installed, the --version command will return a result.
                return process.ExitCode == 0;
            }
        }
        catch (Exception)
        {
            // An exception occurred, which means Git is not installed.
            return false;
        }
    }



    static void ListDirectoryContents(string path)
    {
        try
        {
            string[] directories = Directory.GetDirectories(path);
            string[] files = Directory.GetFiles(path);

            Console.WriteLine($"Directory of {path}\n");

            foreach (var dir in directories)
            {
                Console.WriteLine($"<DIR> {Path.GetFileName(dir)}");
            }

            foreach (var file in files)
            {
                Console.WriteLine($"{Path.GetFileName(file)}");
            }

            Console.WriteLine("\nType 'run [filename]' to execute a file.");

            string input = Console.ReadLine();
            string[] commandArgs = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (commandArgs.Length > 1 && commandArgs[0].ToLower() == "run")
            {
                string filePath = Path.Combine(path, commandArgs[1]);
                RunFile(filePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

