﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace FileHashBackend
{
    public class Finder : IFinder
    {
        public FindResult Find(List<string> foldersToSearch, string checksum, Hasher hasher)
        {
            var fileList = new List<string>();

            foreach (var folderPath in foldersToSearch)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                if (!directoryInfo.Exists)
                {
                    continue;
                }

                foreach (var file in directoryInfo.GetFiles())
                {
                    fileList.Add(file.FullName);
                }
            }

            var combiner = new Combiner(fileList);
            return combiner.FindInCombinations(checksum, hasher);
        }

        public List<string> GetAllFilesInDirectory(List<string> foldersToSearch)
        {
            var fileList = new List<string>();

            foreach (var folderPath in foldersToSearch)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                if (!directoryInfo.Exists)
                {
                    continue;
                }

                foreach (var file in directoryInfo.GetFiles())
                {
                    fileList.Add(file.FullName);
                }
            }

            return fileList;
        }
    }
}
