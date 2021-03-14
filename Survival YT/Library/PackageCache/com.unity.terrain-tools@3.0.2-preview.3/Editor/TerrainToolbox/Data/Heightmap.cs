using System;

namespace UnityEngine.Experimental.TerrainAPI
{
	public class Heightmap
	{
		public enum Flip
		{
			None,
			Horizontal,
			Vertical,
		};

		public enum Mode
		{
			Global,
			Batch,
			Tiles
		};

		public enum Depth
		{
			Bit8 = 1,
			Bit16 = 2
		};

		public enum Format
		{
			PNG, 
			TGA,
			RAW
		};
		
		public readonly Vector2Int Size;
		public int Width => Size.x;
		public int Height => Size.y;
		public float Remap = 1.0f;
		public float Base = 0f;

		private readonly float[,] m_NormalisedHeights;

		#region Height Axis Manipulation
		private void FlipHeightsInPlaceHorizontally()
		{
			int otherX = Width - 1;

			for(int x = 0; x < Width / 2; x++, otherX--)
			{
				for(int y = 0; y < Height; y++)
				{
					float temp = m_NormalisedHeights[y, x];

					m_NormalisedHeights[y, x] = m_NormalisedHeights[y, otherX];
					m_NormalisedHeights[y, otherX] = temp;
				}
			}
		}

		private void FlipHeightsInPlaceVertically()
		{
			int otherY = Height - 1;
			
			for(int y = 0; y < Height / 2; y++, otherY--)
			{
				for(int x = 0; x < Width; x++)
				{
					float temp = m_NormalisedHeights[y, x];

					m_NormalisedHeights[y, x] = m_NormalisedHeights[otherY, x];
					m_NormalisedHeights[otherY, x] = temp;
				}
			}
		}

		public void FlipHeightsInPlace(Flip flip)
		{
			switch(flip)
			{
				case Flip.None:
				{
					break;
				}
				
				case Flip.Horizontal:
				{
					FlipHeightsInPlaceHorizontally();
					break;
				}	
				
				case Flip.Vertical:
				{
					FlipHeightsInPlaceVertically();
					break;
				}				
				
				default:
				{
					throw new ArgumentOutOfRangeException(nameof(flip), flip, null);
				}
			} // End of switch.
		}
		#endregion

		#region Raw Data Conversion
		private static void ConvertInt8ToRawData(int value, byte[] buffer, int bufferIndex)
		{
			buffer[bufferIndex] = (byte)(value & 0xFF);
		}
		
		private static void ConvertInt16ToRawData(int value, byte[] buffer, int bufferIndex)
		{
			buffer[bufferIndex] = (byte)(value & 0xFF);
			buffer[bufferIndex + 1] = (byte)((value >> 8) & 0xFF);
		}
		
		/// <summary>
		/// Converts the specified height-map to a 16-bit raw image file.
		/// </summary>
		/// <returns>The array of bytes to be used.</returns>
		public byte[] ConvertToRawData()
		{
			const int valueSize = 2;
			const float scale = (1 << (valueSize * 8)) - 1;
			int bufferSize = Width * Height * valueSize;
			byte[] buffer = new byte[bufferSize];
			int bufferIndex = 0;

			for(int y = 0; y < Height; y++)
			{
				for(int x = 0; x < Width; x++, bufferIndex += valueSize)
				{
					float normalisedHeight = m_NormalisedHeights[y, x];
					float scaledHeight = normalisedHeight * scale;
					int value = Mathf.RoundToInt(scaledHeight);

					ConvertInt16ToRawData(value, buffer, bufferIndex);
				}
			}

			return buffer;
		}
		
		private static int ConvertRawDataToInt8(byte[] buffer, int bufferIndex)
		{
			int value = buffer[bufferIndex];

			return value;
		}

		private static int ConvertRawDataToInt16(byte[] buffer, int bufferIndex)
		{
			int value = (buffer[bufferIndex + 1] << 8) | buffer[bufferIndex];

			return value;
		}

		private static int[] ConvertFromRawData(byte[] rawData, int valueSize, Func<byte[], int, int> convert)
		{
			int numBytes = rawData.Length;
			int numValues = numBytes / valueSize;
			int[] values = new int[numValues];
			int index = 0;

			for(int i = 0; i < numBytes; i += valueSize)
			{
				values[index++] = convert(rawData, i);
			}

			return values;
		}

		private static float[,] CalculateHeightData(byte[] rawData, int valueSize, Func<byte[], int, int> convert, Vector2Int size)
		{
			int[] rawValues = ConvertFromRawData(rawData, valueSize, convert);
			float[,] heightData = new float[size.y, size.x];
			float scale = (1 << (valueSize * 8)) - 1;
			float oneOverScale = 1.0f / scale;
			int index = 0;

			for(int y = 0; y < size.y; y++)
			{
				for(int x = 0; x < size.x; x++)
				{
					int rawValue = rawValues[index++];
					float height = rawValue * oneOverScale;

					heightData[y, x] = height;
				}
			}

			return heightData;
		}
		#endregion

		#region Height-data Generation
		private float[,] GenerateExactHeightDataFromParent(Heightmap parentHeightmap, Vector2Int offset, Vector2Int size)
		{
			float[,] parentHeightData = parentHeightmap.m_NormalisedHeights;
			float[,] normalisedHeights = new float[size.y, size.x];

			// Copy the height data from our parent height-map...
			for(int y = 0; y < size.y; y++)
			{
				for(int x = 0; x < size.x; x++)
				{
					int parentX = offset.x + x;
					int parentY = offset.y + y;
					float parentHeight = parentHeightData[parentY, parentX];

					m_NormalisedHeights[y, x] = parentHeight;
				}
			}

			return normalisedHeights;
		}

		private float[,] GenerateBlendedHeightDataFromParent(Heightmap parentHeightmap, Vector2Int offset, Vector2Int size)
		{
			float[,] normalisedHeights = new float[size.y + 1, size.x + 1];
			Vector2 parentOffset = new Vector2(0.0f, offset.y);	// - 0.5f);
			int width = size.x;
			int height = size.y;

			// Copy the height data from our parent height-map...
			for(int y = 0; y <= height; y++, parentOffset.y++)
			{
				parentOffset.x = offset.x;	// - 0.5f;
				
				for(int x = 0; x <= width; x++, parentOffset.x++)
				{
					float parentHeight = parentHeightmap.GetNormalisedHeightAt(parentOffset);

					normalisedHeights[y, x] = parentHeight * Remap + Base;
				}
			}

			return normalisedHeights;
		}
		#endregion

		public Heightmap(byte[] rawData, Flip flip)
		{
			int numElements = rawData.Length;
			int size = (int)Mathf.Sqrt(numElements);
			int size2 = size * size;

			// Is this an 8-bit raw image?
			if(size2 == numElements)
			{
				Size = new Vector2Int(size, size);
				m_NormalisedHeights = CalculateHeightData(rawData, 1, ConvertRawDataToInt8, Size);
			}
			else
			{
				numElements /= 2;
				size = (int)Mathf.Sqrt(numElements);
				size2 = size * size;

				// Is this a 16-bit raw image?
				if(size2 == numElements)
				{
					Size = new Vector2Int(size, size);
					m_NormalisedHeights = CalculateHeightData(rawData, 2, ConvertRawDataToInt16, Size);
				}
				else
				{
					throw new InvalidOperationException("Cannot generate a height-map from non-square data.");
				}
			}

			FlipHeightsInPlace(flip);
		}

		public Heightmap(Heightmap parentHeightmap, Vector2Int offset, Vector2Int size, float remap, float baseLevel)
		{
			// Initialise our dimensions and allocate the necessary memory...
			Size = size;
			Remap = remap;
			Base = baseLevel;
			m_NormalisedHeights = GenerateBlendedHeightDataFromParent(parentHeightmap, offset, size);
		}

		public Heightmap(float[,] heights, Flip flip)
		{
			int numRows = heights.GetLength(0);
			int numColumns = heights.GetLength(1);
			
			Size = new Vector2Int(numRows, numColumns);
			m_NormalisedHeights = heights;
			
			FlipHeightsInPlace(flip);
		}

		public Heightmap(Vector2Int size)
		{
			Size = size;
			m_NormalisedHeights = new float[Height, Width];
		}

		/// <summary>
		/// Sets the array of normalised heights at the position specified in the height-map.
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="normalisedHeights"></param>
		public void SetNormalisedHeights(Vector2Int offset, float[,] normalisedHeights)
		{
			int specifiedWidth = normalisedHeights.GetLength(1);
			int specifiedHeight = normalisedHeights.GetLength(0);
			
			for(int x = 0; x < specifiedWidth; x++)
			{
				for(int y = 0; y < specifiedHeight; y++)
				{
					float normalisedHeight = normalisedHeights[y, x];

					m_NormalisedHeights[offset.y + y, offset.x + x] = normalisedHeight;
				}
			}
		}

		/// <summary>
		/// Gets the normalised height at the specified location in the height-map.
		/// </summary>
		/// <param name="offset">The co-ordinates of the height to fetch.</param>
		/// <returns>The normalised height at the location specified.</returns>
		public float GetNormalisedHeightAt(Vector2 offset)
		{
			int x = (offset.x >= Width) ? Width - 1 : (int)offset.x;
			int y = (offset.y >= Height) ? Height - 1 : (int)offset.y;
			float height = m_NormalisedHeights[y, x];

			return height;
		}

		/// <summary>
		/// Apply the data held by this height-map to the specified piece of terrain.
		/// </summary>
		/// <param name="terrain">The terrain to apply the height-map to.</param>
		/// <param name="size">The size of the terrain to be generated,</param>
		public void ApplyTo(Terrain terrain)
		{
			terrain.terrainData.heightmapResolution = Width;
			terrain.terrainData.SetHeights(0, 0, m_NormalisedHeights);
		}

		#region Texture Generation
		/// <summary>
		/// Generate a Texture2D from the current height-map applying a checkerboard effect on the alpha-channel.
		/// </summary>
		/// <param name="textureSize">The size of the texture to generate.</param>
		/// <param name="checkerboardDimensions">The number of tiles on the checkerboard.</param>
		/// <param name="checkerboardAlpha">The alpha of the dark checkerboard tiles.</param>
		/// <returns>The Texture2D generated.</returns>
		public Texture2D ToTexture2D(Vector2Int textureSize, Vector2Int checkerboardDimensions, float checkerboardAlpha)
		{
			const TextureFormat format = TextureFormat.ARGB32;
			const bool mipChain = false;
			const bool linear = true;
			Vector2Int checkerboardSize = new Vector2Int(textureSize.x / checkerboardDimensions.x, textureSize.y / checkerboardDimensions.y);
			Texture2D newTexture = new 	Texture2D(textureSize.x, textureSize.y, format, mipChain, linear);
			float incrementX = (float)Width / textureSize.x;
			float incrementY = (float)Height / textureSize.y;
			float heightmapY = 0.0f;

			// No filtering here - simply read a value for each pixel in the output texture...
			for(int textureY = 0; textureY < textureSize.y; textureY++, heightmapY += incrementY)
			{
				int checkerboardY = textureY / checkerboardSize.y;
				float heightmapX = 0.0f;
				
				for(int textureX = 0; textureX < textureSize.x; textureX++, heightmapX += incrementX)
				{
					int checkerboardX = textureX / checkerboardSize.x;
					int checkerboardIndex = checkerboardX + checkerboardY;
					float normalisedHeight = m_NormalisedHeights[(int)heightmapY, (int)heightmapX];
					float alpha = ((checkerboardIndex & 1) == 0) ? 1.0f : checkerboardAlpha;
					Color color = new Color(normalisedHeight, normalisedHeight, normalisedHeight, alpha);
					
					newTexture.SetPixel(textureX, textureY, color);
				}
			}
			
			newTexture.Apply();
			return newTexture;
		}

		/// <summary>
		/// Generate a Texture2D from the current height-map applying a checkerboard effect on the alpha-channel.
		/// </summary>
		/// <param name="checkerboardDimensions">The number of tiles on the checkerboard.</param>
		/// <param name="checkerboardAlpha">The alpha of the dark checkerboard tiles.</param>
		/// <returns>The Texture2D generated.</returns>
		public Texture2D ToTexture2D(Vector2Int checkerboardDimensions, float checkerboardAlpha)
		{
			Vector2Int textureSize = new Vector2Int(Width, Height);

			return ToTexture2D(textureSize, checkerboardDimensions, checkerboardAlpha);
		}

		/// <summary>
		/// Generate a Texture2D from the current height-map.
		/// </summary>
		/// <returns>The Texture2D generated.</returns>
		public Texture2D ToTexture2D()
		{
			Vector2Int oneByOne = new Vector2Int(1, 1);
				
			return ToTexture2D(oneByOne, 0.5f);
		}
		#endregion
	}
}
