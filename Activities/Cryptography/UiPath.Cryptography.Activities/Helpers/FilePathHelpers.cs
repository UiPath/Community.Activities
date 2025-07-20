using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities.Helpers
{
    public class FilePathHelpers
    {
        public static (string, string, string) GetDefaultFileNameAndLocation(IResource inputFile, string inputFilePath, string outputFileName, bool overwrite, string outputFilePath, string suffix)
        {
            string fileName = string.Empty;
            string filePath = string.Empty;

            if (inputFile != null && !inputFile.IsFolder)
            {
                // Get local file
                var localFile = inputFile.ToLocalResource();
                //Resolve Sync
                Task.Run(async () => await localFile.ResolveAsync()).GetAwaiter().GetResult();

                //take the path from the resource
                inputFilePath = localFile.LocalPath;
                fileName = localFile.FullName;
            }

            if (string.IsNullOrEmpty(outputFileName) && string.IsNullOrEmpty(outputFilePath))
            {
                fileName = Path.GetFileNameWithoutExtension(inputFilePath) + suffix + Path.GetExtension(inputFilePath);

                var directory = Path.GetDirectoryName(inputFilePath);

                if (string.IsNullOrEmpty(directory))
                {
                    filePath = fileName;
                }
                else
                {
                    filePath = Path.Combine(directory, fileName);
                }

                if (outputFilePath == null && !overwrite && File.Exists(filePath))
                {
                    throw new ArgumentException(Resources.FileAlreadyExistsException,
                        Resources.OutputFilePathDisplayName);
                }
            }
            else 
            {
                fileName = outputFileName;
            }

            return (fileName, filePath, inputFilePath);
        }
    }
}
