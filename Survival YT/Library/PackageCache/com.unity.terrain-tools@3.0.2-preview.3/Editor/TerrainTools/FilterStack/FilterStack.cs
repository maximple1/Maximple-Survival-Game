using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class FilterStack : ScriptableObject
    {
        const string k_BufferName0 = "FilterStack.SwapBuffer[0]";
        const string k_BufferName1 = "FilterStack.SwapBuffer[1]";

        /// <summary>
        /// The System.Collections.Generic.List`1 that contains all the Filters for this FilterStack
        /// </summary>
        [SerializeField]
        public List< Filter > filters = new List<Filter>();

        private RTHandle[] swapBuffer = new RTHandle[2];

        void OnEnable()
        {
            // need to check if any filters have been removed for any reason, on load
            filters.RemoveAll( f => f == null );
        }

        /// <summary>
        /// Adds a Filter reference to the end of the FilterStack list of Filters
        /// <param name="filter">The Filter reference to add</param>
        /// </summary>
        public void Add( Filter filter )
        {
            filters.Add( filter );
        }

        /// <summary>
        /// Inserts the specified Filter at the specified index
        /// <param name="index">The index at which the Filter reference should be inserted</param>
        /// <param name="filter">The Filter reference to insert</param>
        /// <exception>Throws an exception if the specified index is not within the valid range</exception>
        /// </summary>
        public void Insert( int index, Filter filter )
        {
            filters.Insert( index, filter );
        }

        /// <summary>
        /// Removes the specified Filter
        /// <param name="filter">The Filter reference to remove</param>
        /// <returns>Returns true if the specified Filter was found and removed; otherwise, returns false.</returns>
        /// </summary>
        public bool Remove( Filter filter )
        {
            return filters.Remove( filter );
        }

        /// <summary>
        /// Removes the Filter at the specified index
        /// <param name="index">The index of the Filter to be removed</param>
        /// <exception>Throws an exception if the specified index is not within the valid range</exception>
        /// </summary>
        public void RemoveAt( int index )
        {
            filters.RemoveAt( index );
        }

        /// <summary>
        /// Evaluate the FilterStack. Composited result will be copied into fc.destinationRenderTexture
        /// <param name="fc">The FilterContext that should be used for composition</param>
        /// <exception>Throws an exception if source or destination RenderTexture is null</exception>
        /// </summary>
        public void Eval(FilterContext fc, RenderTexture source, RenderTexture dest)
        {
            if(dest == null)
            {
                throw new InvalidOperationException("FilterContext::Eval: Source and destination RenderTextures are not properly set up");
            }

            using(new ActiveRenderTextureScope(RenderTexture.active))
            {
                int count = filters.Count;
                int srcIndex = 0;
                int destIndex = 1;

                var descriptor = RTUtils.GetDescriptor(dest.width, dest.height, 0, FilterUtility.defaultFormat);
                swapBuffer[0] = RTUtils.GetTempHandle(descriptor).WithName(k_BufferName0);
                swapBuffer[1] = RTUtils.GetTempHandle(descriptor).WithName(k_BufferName1);
                // ensure the textures are created for compute usage
                swapBuffer[0].RT.Create();
                swapBuffer[1].RT.Create();

                //don't remove this! needed for compute shaders to work correctly.
                Graphics.Blit(Texture2D.whiteTexture, swapBuffer[0]); // TODO: change this to black or source. should build up the mask instead of always multiply
                Graphics.Blit(Texture2D.blackTexture, swapBuffer[1]);

                for( int i = 0; i < count; ++i )
                {
                    var filter = filters[i];
                    if (!filter.enabled) continue;

                    filter.Eval(fc, swapBuffer[srcIndex], swapBuffer[destIndex]);

                    // swap indices
                    destIndex += srcIndex;
                    srcIndex = destIndex - srcIndex;
                    destIndex = destIndex - srcIndex;
                }

                Graphics.Blit(swapBuffer[srcIndex], dest);

                RTUtils.Release(swapBuffer[0]);
                RTUtils.Release(swapBuffer[1]);
                swapBuffer[0] = null;
                swapBuffer[1] = null;
            }
        }

        /// <summary>
        /// Removes all Filters
        /// </summary>
        public void Clear(bool destroy = false)
        {
            if(destroy)
            {
                foreach(var filter in filters)
                {
                    DestroyImmediate(filter);
                }
            }

            filters.Clear();
        }
    }
}