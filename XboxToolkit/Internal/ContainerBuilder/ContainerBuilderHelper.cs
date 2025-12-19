using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XboxToolkit.Internal.ContainerBuilder
{
    internal static class ContainerBuilderHelper
    {
        public static void ScanFolder(string basePath, string relativePath, List<FileEntry> fileEntries, List<DirectoryEntry> directoryEntries)
        {
            var fullPath = string.IsNullOrEmpty(relativePath) ? basePath : Path.Combine(basePath, relativePath);
            
            var dirEntry = new DirectoryEntry { Path = relativePath };
            directoryEntries.Add(dirEntry);

            var files = Directory.GetFiles(fullPath);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var fileName = Path.GetFileName(file);
                var fileRelativePath = string.IsNullOrEmpty(relativePath) ? fileName : Path.Combine(relativePath, fileName).Replace('\\', '/');
                
                fileEntries.Add(new FileEntry
                {
                    RelativePath = fileRelativePath,
                    FullPath = file,
                    Size = (uint)fileInfo.Length
                });
                dirEntry.Files.Add(fileEntries.Last());
            }

            var subdirs = Directory.GetDirectories(fullPath);
            foreach (var subdir in subdirs)
            {
                var dirName = Path.GetFileName(subdir);
                var subdirRelativePath = string.IsNullOrEmpty(relativePath) ? dirName : Path.Combine(relativePath, dirName).Replace('\\', '/');
                ScanFolder(basePath, subdirRelativePath, fileEntries, directoryEntries);
            }
        }

        public static DirectoryEntry BuildDirectoryTree(List<DirectoryEntry> directoryEntries, List<FileEntry> fileEntries, string rootPath)
        {
            var root = directoryEntries.FirstOrDefault(d => d.Path == rootPath);
            if (root == null)
            {
                root = new DirectoryEntry { Path = rootPath };
            }

            // Get files in this directory
            root.Files = fileEntries.Where(f => 
            {
                var fileDir = Path.GetDirectoryName(f.RelativePath)?.Replace('\\', '/') ?? string.Empty;
                if (string.IsNullOrEmpty(rootPath))
                {
                    // Root directory: files with no directory or "." directory
                    return string.IsNullOrEmpty(fileDir) || fileDir == ".";
                }
                // Non-root: files whose directory matches rootPath
                return fileDir == rootPath;
            }).ToList();

            // Get subdirectories
            root.Subdirectories = directoryEntries
                .Where(d => 
                {
                    if (d.Path == rootPath) return false; // Skip self
                    
                    var parentPath = Path.GetDirectoryName(d.Path)?.Replace('\\', '/') ?? string.Empty;
                    if (string.IsNullOrEmpty(rootPath))
                    {
                        // Root directory: subdirectories that are direct children (no '/' in path)
                        // This means the path itself is just the directory name, e.g. "subdir" not "subdir/nested"
                        return !d.Path.Contains('/');
                    }
                    // Non-root: subdirectories whose parent matches rootPath
                    return parentPath == rootPath;
                })
                .Select(d => BuildDirectoryTree(directoryEntries, fileEntries, d.Path))
                .ToList();

            return root;
        }

        public static void CalculateDirectorySizes(DirectoryEntry directory, Dictionary<string, uint> directorySizes, uint sectorSize)
        {
            uint totalSize = 0;

            // Calculate size needed for all entries in this directory
            // Only count entries with non-empty names (matching BuildDirectoryData logic)
            var entries = new List<(bool isDir, string name, uint size)>();
            
            foreach (var file in directory.Files)
            {
                var fileName = Path.GetFileName(file.RelativePath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    entries.Add((false, fileName, file.Size));
                }
            }

            foreach (var subdir in directory.Subdirectories)
            {
                CalculateDirectorySizes(subdir, directorySizes, sectorSize);
                var dirName = Path.GetFileName(subdir.Path);
                if (!string.IsNullOrEmpty(dirName))
                {
                    entries.Add((true, dirName, directorySizes[subdir.Path]));
                }
            }

            // Each entry is: left(2) + right(2) + sector(4) + size(4) + attribute(1) + nameLength(1) + filename
            // Entries must be 4-byte aligned (padded to 4-byte boundary)
            foreach (var entry in entries)
            {
                var nameBytes = Encoding.ASCII.GetBytes(entry.name);
                var entryDataSize = 2 + 2 + 4 + 4 + 1 + 1 + (uint)nameBytes.Length;
                var entrySize = (entryDataSize + 3) & ~3u; // Round up to 4-byte boundary
                totalSize += entrySize;
            }

            // Round up to sector size
            totalSize = Helpers.RoundToMultiple(totalSize, sectorSize);
            directorySizes[directory.Path] = totalSize;
        }

        public static void AllocateDirectorySectors(DirectoryEntry directory, Dictionary<string, uint> directorySizes, SectorAllocator allocator, uint baseSector)
        {
            // Root directory is already allocated, skip it
            if (directory.Path != string.Empty)
            {
                var dirSize = directorySizes[directory.Path];
                var dirSectors = Helpers.RoundToMultiple(dirSize, Constants.XGD_SECTOR_SIZE) / Constants.XGD_SECTOR_SIZE;
                directory.Sector = allocator.AllocateDirectorySectors(dirSectors) - baseSector;
            }

            foreach (var subdir in directory.Subdirectories)
            {
                AllocateDirectorySectors(subdir, directorySizes, allocator, baseSector);
            }
        }

        public static uint GetDirectorySector(string path, DirectoryEntry root, uint baseSector)
        {
            if (root.Path == path)
            {
                return root.Sector + baseSector;
            }

            foreach (var subdir in root.Subdirectories)
            {
                var result = GetDirectorySector(path, subdir, baseSector);
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }

        public static void BuildDirectoryData(DirectoryEntry directory, List<FileEntry> fileEntries, Dictionary<string, uint> directorySizes, Dictionary<string, byte[]> directoryData, uint baseSector)
        {
            var entries = new List<(bool isDir, string name, uint sector, uint size, string path)>();
            
            foreach (var file in directory.Files)
            {
                var fileName = Path.GetFileName(file.RelativePath);
                if (string.IsNullOrEmpty(fileName))
                {
                    // Skip files with empty names
                    continue;
                }
                entries.Add((false, fileName, file.Sector, file.Size, string.Empty));
            }

            foreach (var subdir in directory.Subdirectories)
            {
                // Recursively build subdirectory data first
                BuildDirectoryData(subdir, fileEntries, directorySizes, directoryData, baseSector);
                
                var dirName = Path.GetFileName(subdir.Path);
                if (string.IsNullOrEmpty(dirName))
                {
                    // Skip directories with empty names
                    continue;
                }
                
                // Add subdirectory to entries - even if it's empty, it should still be in the parent's entry list
                // Empty directories will have size = sector size (minimum), and their directory data will be all zeros
                entries.Add((true, dirName, subdir.Sector, directorySizes[subdir.Path], subdir.Path));
            }

            // Sort entries by name for binary tree
            entries.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            // Build binary tree structure
            var dirSize = directorySizes[directory.Path];
            var dirData = new byte[dirSize];
            // Initialize to zeros to ensure uninitialized data doesn't cause issues
            for (int i = 0; i < dirData.Length; i++)
            {
                dirData[i] = 0;
            }
            var offset = 0u;

            // Build binary tree even if entries is empty (will result in all zeros, which is correct for empty directories)
            if (entries.Count > 0)
            {
                BuildBinaryTree(entries, dirData, ref offset, 0, entries.Count - 1, baseSector);
            }
            // If entries.Count == 0, dirData remains all zeros, which is correct for an empty directory

            directoryData[directory.Path] = dirData;
        }

        private static void BuildBinaryTree(List<(bool isDir, string name, uint sector, uint size, string path)> entries, byte[] dirData, ref uint offset, int start, int end, uint baseSector)
        {
            if (start > end)
            {
                return;
            }

            var currentOffset = offset;
            var mid = (start + end) / 2;
            var entry = entries[mid];

            // Calculate entry size (must be 4-byte aligned for Xbox format)
            var entryDataSize = (uint)(2 + 2 + 4 + 4 + 1 + 1 + entry.name.Length);
            var entrySize = (entryDataSize + 3) & ~3u; // Round up to 4-byte boundary
            
            // Ensure name is not empty
            if (string.IsNullOrEmpty(entry.name))
            {
                throw new InvalidOperationException($"Directory entry has empty name at offset {currentOffset}");
            }
            
            var nameBytes = Encoding.ASCII.GetBytes(entry.name);
            if (nameBytes.Length == 0 || nameBytes.Length > 255)
            {
                throw new InvalidOperationException($"Directory entry name has invalid length: {nameBytes.Length}");
            }
            
            // Advance offset past this entry (reserve space, already aligned)
            offset += entrySize;

            // Calculate where children will be written (offsets are in 4-byte units)
            ushort leftOffset = 0xFFFF;
            ushort rightOffset = 0xFFFF;

            if (start < mid)
            {
                // Left child will be written at current offset (already 4-byte aligned)
                leftOffset = (ushort)(offset / 4);
                BuildBinaryTree(entries, dirData, ref offset, start, mid - 1, baseSector);
            }

            if (mid < end)
            {
                // Right child will be written at current offset (after left subtree, already aligned)
                rightOffset = (ushort)(offset / 4);
                BuildBinaryTree(entries, dirData, ref offset, mid + 1, end, baseSector);
            }

            // Write entry at currentOffset (after children, so offsets are correct)
            using (var stream = new MemoryStream(dirData))
            using (var writer = new BinaryWriter(stream))
            {
                stream.Position = currentOffset;
                writer.Write(leftOffset);
                writer.Write(rightOffset);
                writer.Write(entry.sector);
                writer.Write(entry.size);
                writer.Write((byte)(entry.isDir ? 0x10 : 0x00));
                writer.Write((byte)nameBytes.Length);
                writer.Write(nameBytes);
                
                // Pad to 4-byte boundary
                var padding = entrySize - entryDataSize;
                if (padding > 0)
                {
                    writer.Write(new byte[padding]);
                }
            }
        }

        public static void WriteXgdHeader(byte[] sector, uint rootDirSector, uint rootDirSize)
        {
            using (var stream = new MemoryStream(sector))
            using (var writer = new BinaryWriter(stream))
            {
                var magic = Encoding.UTF8.GetBytes(Constants.XGD_IMAGE_MAGIC);
                writer.Write(magic);
                if (magic.Length < 20)
                {
                    writer.Write(new byte[20 - magic.Length]);
                }

                writer.Write(rootDirSector);
                writer.Write(rootDirSize);
                writer.Write(DateTime.Now.ToFileTime());
                writer.Write(new byte[0x7c8]); // Padding
                
                writer.Write(magic); // MagicTail
                if (magic.Length < 20)
                {
                    writer.Write(new byte[20 - magic.Length]);
                }
            }
        }
    }
}

