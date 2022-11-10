namespace Resurgent.UtilityBelt.Library.Utilities
{
    public class XisoScrubber
    {
        private struct TreeNodeInfo
        {
            public uint DirectorySize { get; set; }
            public long DirectoryOffset { get; set; }            
            public uint Offset { get; set; }
            public long StartOffset { get; set; }
        }

        public static void Scrub(string fileIn, string fileOut)
        {
            using var fileStream = new FileStream(fileIn, FileMode.Open);
            using var binaryReader = new BinaryReader(fileStream);

            var dataSectors = GetDataSectors(binaryReader);

            using var binaryWriter = new BinaryWriter(new FileStream(fileOut, FileMode.OpenOrCreate));

            var emptySector = new byte[2048];

            binaryReader.BaseStream.Position = 0x18300000;
            while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
            {
                var sector = (uint)(binaryReader.BaseStream.Position / 2048);
                if (dataSectors.Contains(sector))
                {
                    var sectorBuffer = binaryReader.ReadBytes(2048);
                    binaryWriter.Write(sectorBuffer);
                }
                else
                {
                    binaryReader.BaseStream.Position += 2048;
                    binaryWriter.Write(emptySector);
                }
            }
        }

        public static HashSet<uint> GetDataSectors(BinaryReader binaryReader)
        {
            var dataSectors = new HashSet<uint>();
            var headerSector = (0x18300000U + 0x10000U) / 2048;
            dataSectors.Add(headerSector);
            dataSectors.Add(headerSector + 1);
            binaryReader.BaseStream.Position = 0x18300000 + 0x10000 + 20;

            var rootSector = binaryReader.ReadUInt32();
            var rootSize = binaryReader.ReadUInt32();
            var rootOffset = (long)rootSector * 2048;

            var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectorySize = rootSize,
                    DirectoryOffset = rootOffset,
                    Offset = 0,
                    StartOffset = 0x18300000
                }
            };

            while (treeNodes.Count > 0)
            {
                var currentTreeNode = treeNodes[0];

                var currentOffset = currentTreeNode.StartOffset + currentTreeNode.DirectoryOffset + currentTreeNode.Offset * 4;

                for (var i = currentOffset / 2048; i < (currentOffset / 2048) + ((currentTreeNode.DirectorySize - (currentTreeNode.Offset * 4) + 2048 - 1) / 2048); i++)
                {
                    dataSectors.Add((uint)i);
                }

                if ((currentTreeNode.Offset * 4) >= currentTreeNode.DirectorySize)
                {
                    treeNodes.RemoveAt(0);
                    continue;
                }

                binaryReader.BaseStream.Position = currentOffset;

                var left = binaryReader.ReadUInt16();
                var right = binaryReader.ReadUInt16();
                var sector = binaryReader.ReadUInt32();
                var size = binaryReader.ReadUInt32();
                var attribute = binaryReader.ReadByte();
                var dataOffset = (long)sector * 2048;

                if (left == 0xFFFF)
                {
                    treeNodes.RemoveAt(0);
                    continue;
                }

                if (left != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryOffset = currentTreeNode.DirectoryOffset,
                        Offset = left,
                        StartOffset = currentTreeNode.StartOffset
                    });
                }

                if ((attribute & 0x10) != 0)
                {
                    if (size > 0)
                    {
                        treeNodes.Add(new TreeNodeInfo
                        {
                            DirectorySize = size,
                            DirectoryOffset = dataOffset,
                            Offset = 0,
                            StartOffset = currentTreeNode.StartOffset
                        });
                    }
                }
                else
                {
                    for (var i = (currentTreeNode.StartOffset + dataOffset) / 2048; i < (currentTreeNode.StartOffset + dataOffset) / 2048 + ((size + 2048 - 1) / 2048); i++)
                    {
                        dataSectors.Add((uint)i);
                    }
                }

                if (right != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryOffset = currentTreeNode.DirectoryOffset,
                        Offset = right,
                        StartOffset = currentTreeNode.StartOffset
                    });
                }

                treeNodes.RemoveAt(0);
            }

            return dataSectors;
        }
    }
}