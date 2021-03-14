using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    public interface IValidationTests
    {
        string ValidateAndGenerateUserMessage(Terrain terrain);
    }
}
