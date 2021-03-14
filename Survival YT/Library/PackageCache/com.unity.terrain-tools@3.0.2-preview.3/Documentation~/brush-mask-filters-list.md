## List of Brush Mask Filters

### Abs
Sets all pixels of an existing Brush Mask to their absolute values. An absolute value is a numerical value without regard to its sign. For example, the absolute value of 5 is 5, and the absolute value of -2 is 2. 

### Add
Adds the input value to each pixel in the Brush Mask.

### Aspect
Uses the slope aspect of a heightmap to mask the effect of a chosen Brush, and uses Brush rotation to control the aspect direction. The aspect is the compass direction that a slope faces. This Brush is useful when you want to paint a Texture only on a slope that faces a specific direction.

#### Parameters
| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Strength**     | Controls the strength of the masking effect.                 |
| **Feature Size** | Specifies the scale of Terrain features that affect the mask. |
| **Remap Curve**  | Remaps the concavity input before computing the final mask.  |

### Clamp
Clamps the pixels of a mask to the specified range. Change the X value to specify the low end of the range, and change the Y value to specify the high end of the range.

### Complement
Subtracts each pixel value in the current Brush Mask from the specified constant. To invert the mask results, leave the complement value unchanged as 1.

### Concavity
Uses the concavity of a heightmap to mask the effect of a chosen Brush. 

When you specify **Recessed**, Unity generates a mask based on areas of the Terrain that curve or hollow inward. As a visual representation, concave areas of the heightmap are those where you see a ∪-shape in the cross section.

When you specify **Exposed**, Unity generates a mask based on areas of the Terrain that curve or protrude outward. As a visual representation, convex areas of the heightmap are those where you see a ∩-shape in the cross section.

#### Parameters
| **Property**           | **Description**                                              |
| ---------------------- | ------------------------------------------------------------ |
| **Recessed / Exposed** | Specifies whether to apply the mask based on recessed (concave) or exposed (convex) features. |
| **Strength**           | Controls the strength of the masking effect.                 |
| **Feature Size**       | Specifies the scale of Terrain features that affect the mask. This determines the size of features to which to apply the effect. If you specify a small value, Unity generates a mask based on smaller concave or convex parts. If you specify a large value, there need to be larger features in order for Unity to generate a mask. |
| **Remap Curve**        | Remaps the concavity input before computing the final mask.  |

### Height
Uses the height of the heightmap to mask the effect of the chosen Brush. The range is from 0 to 1, where 0 is the minimum heightmap value (black in the heightmap), and 1 is the maximum heightmap value (white in the heightmap). This is useful when you want to apply an effect only to a specific height range on the Terrain, for example, to paint grass only on areas below a certain height, or to paint snow only on areas above a specific height.

#### Parameters
| **Property**     | **Description**                                          |
| ---------------- | -------------------------------------------------------- |
| **Strength**     | Controls the strength of the masking effect.             |
| **Height Range** | Specifics the height range to which to apply the effect. |
| **Remap Curve**  | Remaps the height input before computing the final mask. |

### Max
Sets all pixels of the current mask to whichever is greater, the current pixel value or the input value. 

### Min
Sets all pixels of the current mask to whichever is smaller, the current pixel value or the input value.

### Multiply
Multiplies all the pixels of the current mask by the input value.

### Negates
Reverses the sign of all pixels in the current mask. For example, 1 becomes -1, 0 remains the same, and -1 becomes 1.

### Noise
Applies noise to the Brush Mask. See the [Noise Settings API documentation](../api/UnityEditor.Experimental.TerrainAPI.html) for more information about the actual settings stored in the Noise Settings Asset.

### Power
Applies an exponential function to each pixel on the Brush Mask. The function is *pow(value, e)*, where *e* is the input value.

### Remap
Remaps each pixel value in the Brush Mask from the **From** range to the **To** range. 

### Slope
Uses the slope (first derivative) of the heightmap to mask the effect of the chosen Brush.

#### Parameters
| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Strength**     | Controls the strength of the masking effect.                 |
| **Feature Size** | Specifies the scale of Terrain features that affect the mask. |
| **Remap Curve** | Remaps the slope input before computing the final mask. This helps you visualize how the Terrain's slope affects the generated mask. The x-axis represents the slope angle, where 0 is equivalent to horizontal, and 1 is equivalent to vertical. |

