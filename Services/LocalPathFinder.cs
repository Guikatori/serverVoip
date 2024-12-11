namespace Services;

using Helpers.ResponseHelper;
    public class LocalPathFinder
    {
        public static string MicrosipPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string targetPath = Path.Combine(appDataPath, "MicroSIP", "microsip.exe");

            if (File.Exists(targetPath))
            {
                Console.WriteLine("Caminho do arquivo encontrado: " + targetPath);
                return targetPath;
            }
            else
            {
                targetPath = SecondTypeOfPathVerification();
                return targetPath;
            }
        }

        public static string SecondTypeOfPathVerification()
        {
            string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string program86x = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            List<string> morePaths = new List<string>
            {
                Path.Combine(programFilesPath, "Microsip", "microsip.exe"),
                Path.Combine(program86x, "Microsip", "microsip.exe")
            };

            foreach (var path in morePaths)
            {
                if (File.Exists(path))
                {
                    Console.WriteLine("Caminho do arquivo encontrado: " + path);
                    return path;
                }
            }
            ResponseHelper.ResponseStatus("Is Not Possible To Find Microsip Path",400);
            return string.Empty;
        }
    }
