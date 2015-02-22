using System;
using GoblinToonShader;

namespace toonShader
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (ToonShaderClass game = new ToonShaderClass())
            {
                game.Run();

            }
           
           
        }
    }
#endif
}

