﻿using System;
using System.IO;

namespace JocysCom.TextToSpeech.Monitor.Audio
{
	public class AudioHelper
	{

		#region WAV Header Writer

		static char[] CHUNK_ID = { 'R', 'I', 'F', 'F' };
		static char[] CHUNK_FORMAT = { 'W', 'A', 'V', 'E' };
		static char[] SUBCHUNK1_ID = { 'f', 'm', 't', ' ' };
		static char[] SUBCHUNK2_ID = { 'd', 'a', 't', 'a' };
		static byte[] AUDIO_FORMAT = new byte[] { 0x01, 0x00 };

		/// <summary>44 Bytes</summary>
		public const int WavHeadSize = 44;

		/// <summary>
		/// Header size is 44 bytes.
		/// </summary>
		public static byte[] GetWavHead(int byteSize, int sampleRate, int bitsPerSample, int channelCount)
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			int bytesPerSample = bitsPerSample / 8;
			int byteRate = sampleRate * channelCount * bytesPerSample;
			int blockAlign = channelCount * bytesPerSample;
			int subChunk1Size = 16;
			bw.Write(CHUNK_ID, 0, CHUNK_ID.Length);
			bw.Write(PackageInt(byteSize + 42, 4), 0, 4);
			bw.Write(CHUNK_FORMAT, 0, CHUNK_FORMAT.Length);
			bw.Write(SUBCHUNK1_ID, 0, SUBCHUNK1_ID.Length);
			bw.Write(PackageInt(subChunk1Size, 4), 0, 4);
			bw.Write(AUDIO_FORMAT, 0, AUDIO_FORMAT.Length);
			bw.Write(PackageInt(channelCount, 2), 0, 2);
			bw.Write(PackageInt(sampleRate, 4), 0, 4);
			bw.Write(PackageInt(byteRate, 4), 0, 4);
			bw.Write(PackageInt(blockAlign, 2), 0, 2);
			bw.Write(PackageInt(bytesPerSample * 8), 0, 2);
			//targetStream.Write(PackageInt(0,2), 0, 2); // Extra param size
			bw.Write(SUBCHUNK2_ID, 0, SUBCHUNK2_ID.Length);
			bw.Write(PackageInt(byteSize, 4), 0, 4);
			var bytes = ms.ToArray();
			bw.Dispose();
			return bytes;
		}


		public static void GetInfo(byte[] wavHead, out int sampleRate, out int bitsPerSample, out int channelCount)
		{
			channelCount = BitConverter.ToInt16(wavHead, 22);
			sampleRate = BitConverter.ToInt32(wavHead, 24);
			bitsPerSample = BitConverter.ToInt16(wavHead, 34);
		}

		/// <summary>
		/// Get duration in miliseconds.
		/// </summary>
		public static int GetDuration(int wavDataSize, int sampleRate, int bitsPerSample, int channelCount)
		{
			var duration = ((decimal)wavDataSize * 8m) / (decimal)channelCount / (decimal)sampleRate / (decimal)bitsPerSample * 1000m;
			return (int)duration;
		}

		public static int GetSilenceByteCount(int sampleRate, int bitsPerSample, int channelCount, int milliseconds)
		{
			int bytesPerSample = bitsPerSample / 8;
			// Get amount of bytes needed to write for audio with one channel and one byte per sample.
			var dataLength = (int)((decimal)sampleRate * ((decimal)milliseconds / 1000));
			// Get total amount of bytes written.
			int totalBytes = dataLength * channelCount * bytesPerSample;
			return totalBytes;
		}

		public static int WriteSilenceBytes(System.IO.BinaryWriter writer, int sampleRate, int bitsPerSample, int channelCount, int milliseconds)
		{
			int bytesPerSample = bitsPerSample / 8;
			// Get amount of bytes needed to write for audio with one channel and one byte per sample.
			var dataLength = (int)((decimal)sampleRate * ((decimal)milliseconds / 1000m));
			for (int i = 0; i < dataLength; i++)
			{
				if (bytesPerSample == 1)
				{
					for (int c = 0; c < channelCount; c++) Write8bit(writer, 0F, 0F);
				}
				else
				{
					for (int c = 0; c < channelCount; c++) Write16bit(writer, 0F, 0F);
				}
			}
			// Get total amount of bytes written.
			int totalBytes = dataLength * channelCount * bytesPerSample;
			return totalBytes;
		}

		/// <summary>Convert byte to one double in the range -1 to 1</summary>
		public static float BytesToNormalized_8(byte firstByte)
		{
			return (float)(firstByte + sbyte.MinValue) / 128F;
		}

		/// <summary>Convert a float value into two bytes (use k as conversion value and not Int16.MaxValue to avoid peaks)</summary>
		public static void Write8bit(BinaryWriter writer, float value, float k)
		{
			sbyte s = (sbyte)(value * k);
			// Write byte.
			writer.Write((byte)(s - sbyte.MinValue));
		}

		/// <summary>Convert two bytes to one double in the range -1 to 1</summary>
		public static float BytesToNormalized_16(byte firstByte, byte secondByte)
		{
			// convert two bytes to one short (little endian)
			short s = (short)((secondByte << 8) | firstByte);
			// convert to range from -1 to (just below) 1
			return (float)s / 32678f;
		}

		/// <summary>Convert a float value into two bytes (use k as conversion value and not Int16.MaxValue to avoid peaks)</summary>
		public static void Write16bit(BinaryWriter writer, float value, float k)
		{
			short s = (short)(value * k);
			// Write first byte.
			writer.Write((byte)(s & 0xFF));
			// Write second byte.
			writer.Write((byte)(s >> 8));
		}

		static byte[] PackageInt(int source, int length = 2)
		{
			if ((length != 2) && (length != 4)) throw new ArgumentException("Length must be either 2 or 4", "length");
			var retVal = new byte[length];
			retVal[0] = (byte)(source & 0xFF);
			retVal[1] = (byte)((source >> 8) & 0xFF);
			if (length == 4)
			{
				retVal[2] = (byte)((source >> 0x10) & 0xFF);
				retVal[3] = (byte)((source >> 0x18) & 0xFF);
			}
			return retVal;
		}

		#endregion

	}
}
