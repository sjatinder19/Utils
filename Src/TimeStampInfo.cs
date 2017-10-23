/*********************************************************************************
*  Copyright(c) 2017 Image Stream Medical, Inc. All rights reserved.             * 
*  Replication or redistribution of Image Stream Medical Software is prohibited, *
*  without the prior written consent of Image Stream Medical, Inc.               *
*                                                                                *
*  Author: Image Stream Medical Inc.                                             *
*  Original Copyright Date:  10/24/2017                                          *
*********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsmMp4Util
{
    public class TimeStampInfo
    {
        class DtsInfo
        {
            internal int m_iBuffSize = 0;
            internal int m_iEntryCount = 0;
            internal byte[] m_aarDtsInfo = null;// used by parser while reading data from file     
            internal void Init(int iCount)
            {
                m_iEntryCount = iCount;
                m_iBuffSize = m_iEntryCount * 8;
                m_aarDtsInfo = new byte[m_iBuffSize];
            }

            internal void DeInit()
            {
                m_aarDtsInfo = null;
                m_iBuffSize = 0;
                m_iEntryCount = 0;
            }

            internal Tuple<uint, uint> GetDeltaInfo(int iIndex)
            {
                // Invalid index
                if (iIndex < 0 || iIndex >= m_iEntryCount)
                    return null;

                int iOffset = iIndex * 8;
                uint uiSampleCount = Utils.ReadUInt32(m_aarDtsInfo, (ulong)iOffset);
                iOffset += 4;
                uint uiSampleDelta = Utils.ReadUInt32(m_aarDtsInfo, (ulong)iOffset);

                return new Tuple<uint, uint>(uiSampleCount, uiSampleDelta);
            }
        }

        enum eTrackType
        {
            unknown,
            audio,
            video
        }

        class TrackInfo
        {
            internal uint m_nTrackSampleCount = 0;
            internal eTrackType m_TrackType = eTrackType.unknown;
            internal uint m_uiTimeScale = 0;
            internal ulong m_ulDuration = 0;
            internal DtsInfo m_DTSInfo = new DtsInfo();
        }

        TrackInfo m_VideoTrackInfo = null;

        public double FileDurationSecs
        {
            get
            {
                return (double)m_VideoTrackInfo.m_ulDuration / m_VideoTrackInfo.m_uiTimeScale;
            }
        }

        double MaxSecsForSeekTable
        {
            get;
            set;
        }

        public int GetSeekTable(string strFileName, double dbMaxSecsForSeekTable, out double dbAverageFrameDurationSecs, out string strSeekTable, out string strErr)
        {
            strErr = string.Empty;
            strSeekTable = string.Empty;
            dbAverageFrameDurationSecs = 0.0;
            try
            {
                MaxSecsForSeekTable = dbMaxSecsForSeekTable;

                if (!File.Exists(strFileName))
                {
                    strErr = "File does not exist. " + strFileName;
                    return 1;
                }


                using (FileStream fileReadStream = new FileStream(strFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (BinaryReader binaryReader = new BinaryReader(fileReadStream))
                    {
                        // Validates and moves moov atom to start of file
                        ParseFile(binaryReader);
                    }
                }

                // Pass timestamp info
                if (m_VideoTrackInfo == null)
                    throw new Exception("Failed to parse video track");

                double dbTrackDurSecs = (double)m_VideoTrackInfo.m_ulDuration / m_VideoTrackInfo.m_uiTimeScale;
                dbAverageFrameDurationSecs = dbTrackDurSecs / m_VideoTrackInfo.m_nTrackSampleCount;

                if (MaxSecsForSeekTable >= dbTrackDurSecs)
                {
                    StringBuilder seekTableBuilder = new StringBuilder();
                    double dbFrameTimeSecs = 0;
                    for (int nIndex = 0; nIndex < m_VideoTrackInfo.m_DTSInfo.m_iEntryCount; nIndex++)
                    {
                        Tuple<uint, uint> deltaInfo = m_VideoTrackInfo.m_DTSInfo.GetDeltaInfo(nIndex);
                        if (deltaInfo == null)
                            continue; // This should not happen

                        for (uint uiSampleIndex = 0; uiSampleIndex < deltaInfo.Item1; uiSampleIndex++)
                        {
                            double dbSampleDurationSecs = (double)deltaInfo.Item2 / m_VideoTrackInfo.m_uiTimeScale;

                            seekTableBuilder.AppendFormat("{0:0.000},", dbFrameTimeSecs);

                            dbFrameTimeSecs += dbSampleDurationSecs;
                        }
                    }
                    strSeekTable = seekTableBuilder.ToString();
                }

                return 0;
            }
            catch (Exception ex)
            {
                strErr = string.Format("ParseFile failed. Error - {0}", ex.Message);
                return 1;
            }
        }

        private void ReadHead(BinaryReader reader, UInt64 uiFileOffset, out string strAtomId, ref ulong uiAtomLength)
        {
            byte[] MpegBoxBuff = Utils.ReadBytes(ref reader, uiFileOffset, (int)Utils.HEADER_LENGTH);

            char[] Atom_ID;
            Utils.ReadAtom(MpegBoxBuff, ref uiAtomLength, out Atom_ID);
            if (uiAtomLength == 0)
            {
                uiAtomLength = (UInt64)reader.BaseStream.Length;
            }
            else if (uiAtomLength == 1)
            {
                MpegBoxBuff = Utils.ReadBytes(ref reader, uiFileOffset + Utils.HEADER_LENGTH, 8);
                uiAtomLength = (ulong)Utils.ReadInt64(MpegBoxBuff, 0);
            }
            else
            {
                // update file offset
                uiAtomLength = System.Math.Max(uiAtomLength, Utils.HEADER_LENGTH);
            }

            string sAtomID = new string(Atom_ID);
            strAtomId = sAtomID.ToLower();
        }

        private void ParseFile(BinaryReader binaryReader)
        {
            UInt64 uiFileOffset = 0;
            UInt64 uiAtomLength = 0;

            while ((long)uiFileOffset < binaryReader.BaseStream.Length)
            {
                string strAtomId;
                ReadHead(binaryReader, uiFileOffset, out strAtomId, ref uiAtomLength);

                switch (strAtomId)
                {
                    case "moov":
                        {
                            ParseMoovAtom(binaryReader, uiFileOffset + Utils.HEADER_LENGTH, uiAtomLength - Utils.HEADER_LENGTH);
                            break;
                        }
                }
                uiFileOffset += uiAtomLength;
            }
        }

        void ParseMoovAtom(BinaryReader reader, ulong ulOffset, ulong ulLength)
        {

            UInt64 uiFileOffset = ulOffset;
            UInt64 uiFileEnd = ulOffset + ulLength;
            UInt64 uiAtomLength = 0;
            while (uiFileOffset < uiFileEnd)
            {
                string strAtomId;
                ReadHead(reader, uiFileOffset, out strAtomId, ref uiAtomLength);

                switch (strAtomId)
                {
                    case "trak":
                        {
                            TrackInfo info = new TrackInfo();
                            ParseTrackAtom(reader, uiFileOffset + Utils.HEADER_LENGTH, uiAtomLength - Utils.HEADER_LENGTH, ref info);

                            if (info.m_TrackType == eTrackType.video)
                            {
                                m_VideoTrackInfo = info; // Store video track info
                            }

                            break;
                        }
                }

                uiFileOffset += uiAtomLength;
            }

        }

        void ParseTrackAtom(BinaryReader reader, ulong ulOffset, ulong ulLength, ref TrackInfo info)
        {

            UInt64 uiFileOffset = ulOffset;
            UInt64 uiFileEnd = ulOffset + ulLength;
            UInt64 uiAtomLength = 0;

            while (uiFileOffset < uiFileEnd)
            {
                string strAtomId;
                ReadHead(reader, uiFileOffset, out strAtomId, ref uiAtomLength);

                switch (strAtomId)
                {
                    case "mdia":
                    case "minf":
                        {
                            // Recursive call
                            ParseTrackAtom(reader, uiFileOffset + Utils.HEADER_LENGTH, uiAtomLength - Utils.HEADER_LENGTH, ref info);
                            break;
                        }

                    case "stbl":
                        {
                            double dbTrackDurSecs = (double)info.m_ulDuration / info.m_uiTimeScale;
                            if (info.m_TrackType == eTrackType.video) // This is video track , parse time
                            {
                                //recuresive call for video
                                ParseTrackAtom(reader, uiFileOffset + Utils.HEADER_LENGTH, uiAtomLength - Utils.HEADER_LENGTH, ref info);
                            }
                            break;
                        }

                    case "mdhd":
                        {
                            UInt64 uiSeekOffset = uiFileOffset + Utils.HEADER_LENGTH;
                            byte[] tempBuff = Utils.ReadBytes(ref reader, uiSeekOffset, 4);

                            uint uiVersion = tempBuff[0];
                            uiSeekOffset += 4;

                            if (uiVersion == 0)
                            {
                                uiSeekOffset += 4;
                                uiSeekOffset += 4;
                            }
                            else
                            {
                                uiSeekOffset += 8;
                                uiSeekOffset += 8;
                            }

                            info.m_uiTimeScale = Utils.ReadUInt32(ref reader, uiSeekOffset);
                            uiSeekOffset += 4;

                            if (uiVersion == 0)
                            {
                                info.m_ulDuration = Utils.ReadUInt32(ref reader, uiSeekOffset);
                                uiSeekOffset += 4;
                            }
                            else
                            {
                                info.m_ulDuration = Utils.ReadUInt64(ref reader, uiSeekOffset);
                                uiSeekOffset += 8;
                            }

                            break;
                        }

                    case "hdlr":
                        {
                            UInt64 uiSeekOffset = uiFileOffset + Utils.HEADER_LENGTH;
                            uiSeekOffset += 4;
                            uiSeekOffset += 4;

                            char[] handler = Utils.ReadChars(ref reader, uiSeekOffset, 4);
                            uiSeekOffset += 4;

                            switch (new string(handler))
                            {
                                case "vide":
                                    {
                                        info.m_TrackType = eTrackType.video;
                                        break;
                                    }

                                case "soun":
                                    {
                                        info.m_TrackType = eTrackType.audio;
                                        break;
                                    }
                            }
                            break;
                        }

                    case "stts": // Timestamp info
                        {
                            UInt64 uiSeekOffset = uiFileOffset + Utils.HEADER_LENGTH;
                            byte[] tempBuff = new byte[4];

                            uint uiVersion = Utils.ReadUInt32(ref reader, uiSeekOffset);
                            uiSeekOffset += 4;

                            uint uiEntryCount = Utils.ReadUInt32(ref reader, uiSeekOffset);
                            uiSeekOffset += 4;

                            info.m_DTSInfo.Init((int)uiEntryCount);
                            int iBuffSize = (int)uiEntryCount * 8;
                            reader.BaseStream.Seek((long)uiSeekOffset, SeekOrigin.Begin);
                            reader.Read(info.m_DTSInfo.m_aarDtsInfo, 0, iBuffSize);

                            break;
                        }

                    case "stsz": // Sample count
                        {
                            UInt64 uiSeekOffset = uiFileOffset + Utils.HEADER_LENGTH;
                            byte[] tempBuff = new byte[4];

                            uiSeekOffset += 4;
                            uiSeekOffset += 4;

                            info.m_nTrackSampleCount = Utils.ReadUInt32(ref reader, uiSeekOffset);
                            uiSeekOffset += 4;

                            break;
                        }
                }

                uiFileOffset += uiAtomLength;
            }
        }        
    }
}
