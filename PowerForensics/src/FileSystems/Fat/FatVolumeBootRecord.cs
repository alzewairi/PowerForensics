﻿using System;
using System.Text;
using PowerForensics.Generic;
using PowerForensics.Utilities;

namespace PowerForensics.Fat
{
    public class FatVolumeBootRecord : VolumeBootRecord
    {
        #region Properties

        //private readonly string Volume;
        public readonly string FatType;
        public readonly string BS_OEMName;
        public readonly byte BPB_NumberOfFATs;
        public readonly ushort BPB_RootEntryCount;
        public readonly ushort BPB_TotalSector16;
        public readonly MEDIA_DESCRIPTOR BPB_Media;
        public readonly ushort BPB_FatSize16;
        public readonly uint BPB_TotalSector32;
        public readonly uint BPB_FatSize32;
        public readonly ushort BPB_ExtFlags;
        public readonly ushort BPB_FileSystemVersion;
        public readonly uint BPB_RootCluster;
        public readonly ushort BPB_FileSytemInfo;
        public readonly ushort BPB_BackupBootSector;
        public readonly byte BS_DriveNumber;     
        public readonly byte BS_BootSignature;
        public readonly uint BS_VolumeId;
        public readonly string BS_VolumeLabel;
        public readonly string BS_FileSystemType;

        #endregion Properties

        #region Constructors

        internal FatVolumeBootRecord(byte[] bytes) //, string volume)
        {
            //Volume = volume;
            BS_OEMName = Encoding.ASCII.GetString(bytes, 3, 8);
            BytesPerSector = BitConverter.ToUInt16(bytes, 11);
            SectorsPerCluster = bytes[13];
            BytesPerCluster = BytesPerSector * SectorsPerCluster;
            ReservedSectors = BitConverter.ToUInt16(bytes, 14);
            BPB_NumberOfFATs = bytes[16];
            BPB_RootEntryCount = BitConverter.ToUInt16(bytes, 17);
            BPB_TotalSector16 = BitConverter.ToUInt16(bytes, 19);
            BPB_Media = (MEDIA_DESCRIPTOR)bytes[21];
            BPB_FatSize16 = BitConverter.ToUInt16(bytes, 22);
            SectorsPerTrack = BitConverter.ToUInt16(bytes, 24);
            NumberOfHeads = BitConverter.ToUInt16(bytes, 26);
            HiddenSectors = BitConverter.ToUInt32(bytes, 28);
            BPB_TotalSector32 = BitConverter.ToUInt32(bytes, 32);

            //At this point, the BPB/boot sector for FAT12 and FAT16 differs from the BPB/boot sector for FAT32.
            //The first table shows the structure for FAT12 and FAT16 starting at offset 36 of the boot sector.
            //Fat12 and Fat16 Structure Starting at Offset 36
            FatType = GetFatType(bytes);

            if (FatType == "FAT12" || FatType == "FAT16")
            {
                /* Volume is FAT12 or FAT16 */
                BS_DriveNumber = bytes[36];
                BS_BootSignature = bytes[38];
                BS_VolumeId = BitConverter.ToUInt32(bytes, 39);
                BS_VolumeLabel = Encoding.ASCII.GetString(bytes, 43, 11);
                BS_FileSystemType = Encoding.ASCII.GetString(bytes, 54, 8);
                CodeSection = Helper.GetSubArray(bytes, 62, 450);
            }
            else
            {
                /* Volume is FAT32 */
                BPB_FatSize32 = BitConverter.ToUInt32(bytes, 36);
                BPB_ExtFlags = BitConverter.ToUInt16(bytes, 40);
                BPB_FileSystemVersion = BitConverter.ToUInt16(bytes, 42);
                BPB_RootCluster = BitConverter.ToUInt32(bytes, 44);
                BPB_FileSytemInfo = BitConverter.ToUInt16(bytes, 48);
                BPB_BackupBootSector = BitConverter.ToUInt16(bytes, 50);
                BS_DriveNumber = bytes[64];
                BS_BootSignature = bytes[66];
                BS_VolumeId = BitConverter.ToUInt32(bytes, 67);
                BS_VolumeLabel = Encoding.ASCII.GetString(bytes, 71, 11);
                BS_FileSystemType = Encoding.ASCII.GetString(bytes, 82, 8);
                CodeSection = Helper.GetSubArray(bytes, 90, 422);
            }
        }

        #endregion Constructors

        #region HelperMethods

        private static string GetFatType(byte[] bytes)
        {
            ushort BPB_BytesPerSector = BitConverter.ToUInt16(bytes, 11);
            byte BPB_SectorPerCluster = bytes[13];
            ushort BPB_ReservedSectorCount = BitConverter.ToUInt16(bytes, 14);
            byte BPB_NumberOfFATs = bytes[16];
            ushort BPB_RootEntryCount = BitConverter.ToUInt16(bytes, 17);
            ushort BPB_FatSize16 = BitConverter.ToUInt16(bytes, 22);
            ushort BPB_TotalSector16 = BitConverter.ToUInt16(bytes, 19);
            uint BPB_TotalSector32 = BitConverter.ToUInt32(bytes, 32);

            uint RootDirSectors = (((uint)BPB_RootEntryCount * 32) + ((uint)BPB_BytesPerSector - 1)) / BPB_BytesPerSector;

            uint FATSz = 0;
            if (BPB_FatSize16 != 0)
            {
                FATSz = BPB_FatSize16;
            }
            else
            {
                FATSz = BitConverter.ToUInt32(bytes, 36);
            }

            uint TotSec = 0;
            if (BPB_TotalSector16 != 0)
            {
                TotSec = BPB_TotalSector16;
            }
            else
            {
                TotSec = BPB_TotalSector32;
            }

            uint DataSec = TotSec - (BPB_ReservedSectorCount + (BPB_NumberOfFATs * FATSz) + RootDirSectors);
            uint CountofClusters = DataSec / BPB_SectorPerCluster;

            if (CountofClusters < 4085)
            {
                /* Volume is FAT12 */
                return "FAT12";
            }
            else if (CountofClusters < 65525)
            {
                /* Volume is FAT16 */
                return "FAT16";
            }
            else
            {
                /* Volume is FAT32 */
                return "FAT32";
            }

        }

        #endregion HelperMethods
    }
}
