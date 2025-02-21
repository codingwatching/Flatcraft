using System;
using System.Xml.Serialization;
using UnityEngine;

namespace LibNoise
{
    /// <summary>
    ///     Provides a two-dimensional noise map.
    /// </summary>
    /// <remarks>
    ///     This covers most of the functionality from LibNoise's noiseutils library, but
    ///     the method calls might not be the same. See the tutorials project if you're wondering
    ///     which calls are equivalent.
    /// </remarks>
    public class Noise2D : IDisposable
    {
        #region Indexers

        /// <summary>
        ///     Gets or sets a value in the noise map by its position.
        /// </summary>
        /// <param name="x">The position on the x-axis.</param>
        /// <param name="y">The position on the y-axis.</param>
        /// <param name="isCropped">Indicates whether to select the cropped (default) or uncropped noise map data.</param>
        /// <returns>The corresponding value.</returns>
        public float this[int x, int y, bool isCropped = true]
        {
            get
            {
                if (isCropped)
                {
                    if (x < 0 && x >= Width)
                        throw new ArgumentOutOfRangeException("Invalid x position");
                    if (y < 0 && y >= Height)
                        throw new ArgumentOutOfRangeException("Invalid y position");
                    return _data[x, y];
                }

                if (x < 0 && x >= _ucWidth)
                    throw new ArgumentOutOfRangeException("Invalid x position");
                if (y < 0 && y >= _ucHeight)
                    throw new ArgumentOutOfRangeException("Invalid y position");
                return _ucData[x, y];
            }
            set
            {
                if (isCropped)
                {
                    if (x < 0 && x >= Width)
                        throw new ArgumentOutOfRangeException("Invalid x position");
                    if (y < 0 && y >= Height)
                        throw new ArgumentOutOfRangeException("Invalid y position");
                    _data[x, y] = value;
                }
                else
                {
                    if (x < 0 && x >= _ucWidth)
                        throw new ArgumentOutOfRangeException("Invalid x position");
                    if (y < 0 && y >= _ucHeight)
                        throw new ArgumentOutOfRangeException("Invalid y position");
                    _ucData[x, y] = value;
                }
            }
        }

        #endregion

        #region Constants

        public static readonly double South = -90.0;
        public static readonly double North = 90.0;
        public static readonly double West = -180.0;
        public static readonly double East = 180.0;
        public static readonly double AngleMin = -180.0;
        public static readonly double AngleMax = 180.0;
        public static readonly double Left = -1.0;
        public static readonly double Right = 1.0;
        public static readonly double Top = -1.0;
        public static readonly double Bottom = 1.0;

        #endregion

        #region Fields

        private float[,] _data;
        private readonly int _ucWidth;
        private readonly int _ucHeight;
        private readonly int _ucBorder = 1; // Border size of extra noise for uncropped data.

        private readonly float[,] _ucData;
        // Uncropped data. This has a border of extra noise data used for calculating normal map edges.

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of Noise2D.
        /// </summary>
        protected Noise2D()
        {
        }

        /// <summary>
        ///     Initializes a new instance of Noise2D.
        /// </summary>
        /// <param name="size">The width and height of the noise map.</param>
        public Noise2D(int size)
            : this(size, size)
        {
        }

        /// <summary>
        ///     Initializes a new instance of Noise2D.
        /// </summary>
        /// <param name="size">The width and height of the noise map.</param>
        /// <param name="generator">The generator module.</param>
        public Noise2D(int size, ModuleBase generator)
            : this(size, size, generator)
        {
        }

        /// <summary>
        ///     Initializes a new instance of Noise2D.
        /// </summary>
        /// <param name="width">The width of the noise map.</param>
        /// <param name="height">The height of the noise map.</param>
        /// <param name="generator">The generator module.</param>
        public Noise2D(int width, int height, ModuleBase generator = null)
        {
            Generator = generator;
            Width = width;
            Height = height;
            _data = new float[width, height];
            _ucWidth = width + _ucBorder * 2;
            _ucHeight = height + _ucBorder * 2;
            _ucData = new float[width + _ucBorder * 2, height + _ucBorder * 2];
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the constant value at the noise maps borders.
        /// </summary>
        public float Border { get; set; } = float.NaN;

        /// <summary>
        ///     Gets or sets the generator module.
        /// </summary>
        public ModuleBase Generator { get; set; }

        /// <summary>
        ///     Gets the height of the noise map.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        ///     Gets the width of the noise map.
        /// </summary>
        public int Width { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Gets normalized noise map data with all values in the set of {0..1}.
        /// </summary>
        /// <param name="isCropped">Indicates whether to select the cropped (default) or uncropped noise map data.</param>
        /// <param name="xCrop">This value crops off data from the right of the noise map data.</param>
        /// <param name="yCrop">This value crops off data from the bottom of the noise map data.</param>
        /// <returns>The normalized noise map data.</returns>
        public float[,] GetNormalizedData(bool isCropped = true, int xCrop = 0, int yCrop = 0)
        {
            return GetData(isCropped, xCrop, yCrop, true);
        }

        /// <summary>
        ///     Gets noise map data.
        /// </summary>
        /// <param name="isCropped">Indicates whether to select the cropped (default) or uncropped noise map data.</param>
        /// <param name="xCrop">This value crops off data from the right of the noise map data.</param>
        /// <param name="yCrop">This value crops off data from the bottom of the noise map data.</param>
        /// <param name="isNormalized">Indicates whether to normalize noise map data.</param>
        /// <returns>The noise map data.</returns>
        public float[,] GetData(bool isCropped = true, int xCrop = 0, int yCrop = 0, bool isNormalized = false)
        {
            int width, height;
            float[,] data;
            if (isCropped)
            {
                width = Width;
                height = Height;
                data = _data;
            }
            else
            {
                width = _ucWidth;
                height = _ucHeight;
                data = _ucData;
            }

            width -= xCrop;
            height -= yCrop;
            float[,] result = new float[width, height];
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float sample;
                if (isNormalized)
                    sample = (data[x, y] + 1) / 2;
                else
                    sample = data[x, y];
                result[x, y] = sample;
            }

            return result;
        }

        /// <summary>
        ///     Clears the noise map.
        /// </summary>
        /// <param name="value">The constant value to clear the noise map with.</param>
        public void Clear(float value = 0f)
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                _data[x, y] = value;
        }

        /// <summary>
        ///     Generates a planar projection of a point in the noise map.
        /// </summary>
        /// <param name="x">The position on the x-axis.</param>
        /// <param name="y">The position on the y-axis.</param>
        /// <returns>The corresponding noise map value.</returns>
        private double GeneratePlanar(double x, double y)
        {
            return Generator.GetValue(x, 0.0, y);
        }

        /// <summary>
        ///     Generates a non-seamless planar projection of the noise map.
        /// </summary>
        /// <param name="left">The clip region to the left.</param>
        /// <param name="right">The clip region to the right.</param>
        /// <param name="top">The clip region to the top.</param>
        /// <param name="bottom">The clip region to the bottom.</param>
        /// <param name="isSeamless">Indicates whether the resulting noise map should be seamless.</param>
        public void GeneratePlanar(double left, double right, double top, double bottom, bool isSeamless = true)
        {
            if (right <= left || bottom <= top)
                throw new ArgumentException("Invalid right/left or bottom/top combination");
            if (Generator == null)
                throw new ArgumentNullException("Generator is null");
            double xe = right - left;
            double ze = bottom - top;
            double xd = xe / ((double) Width - _ucBorder);
            double zd = ze / ((double) Height - _ucBorder);
            double xc = left;
            for (int x = 0; x < _ucWidth; x++)
            {
                double zc = top;
                for (int y = 0; y < _ucHeight; y++)
                {
                    float fv;
                    if (isSeamless)
                    {
                        fv = (float) GeneratePlanar(xc, zc);
                    }
                    else
                    {
                        double swv = GeneratePlanar(xc, zc);
                        double sev = GeneratePlanar(xc + xe, zc);
                        double nwv = GeneratePlanar(xc, zc + ze);
                        double nev = GeneratePlanar(xc + xe, zc + ze);
                        double xb = 1.0 - (xc - left) / xe;
                        double zb = 1.0 - (zc - top) / ze;
                        double z0 = Utils.InterpolateLinear(swv, sev, xb);
                        double z1 = Utils.InterpolateLinear(nwv, nev, xb);
                        fv = (float) Utils.InterpolateLinear(z0, z1, zb);
                    }

                    _ucData[x, y] = fv;
                    if (x >= _ucBorder && y >= _ucBorder && x < Width + _ucBorder &&
                        y < Height + _ucBorder)
                        _data[x - _ucBorder, y - _ucBorder] = fv; // Cropped data
                    zc += zd;
                }

                xc += xd;
            }
        }

        /// <summary>
        ///     Generates a cylindrical projection of a point in the noise map.
        /// </summary>
        /// <param name="angle">The angle of the point.</param>
        /// <param name="height">The height of the point.</param>
        /// <returns>The corresponding noise map value.</returns>
        private double GenerateCylindrical(double angle, double height)
        {
            double x = Math.Cos(angle * Mathf.Deg2Rad);
            double y = height;
            double z = Math.Sin(angle * Mathf.Deg2Rad);
            return Generator.GetValue(x, y, z);
        }

        /// <summary>
        ///     Generates a cylindrical projection of the noise map.
        /// </summary>
        /// <param name="angleMin">The maximum angle of the clip region.</param>
        /// <param name="angleMax">The minimum angle of the clip region.</param>
        /// <param name="heightMin">The minimum height of the clip region.</param>
        /// <param name="heightMax">The maximum height of the clip region.</param>
        public void GenerateCylindrical(double angleMin, double angleMax, double heightMin, double heightMax)
        {
            if (angleMax <= angleMin || heightMax <= heightMin)
                throw new ArgumentException("Invalid angle or height parameters");
            if (Generator == null)
                throw new ArgumentNullException("Generator is null");
            double ae = angleMax - angleMin;
            double he = heightMax - heightMin;
            double xd = ae / ((double) Width - _ucBorder);
            double yd = he / ((double) Height - _ucBorder);
            double ca = angleMin;
            for (int x = 0; x < _ucWidth; x++)
            {
                double ch = heightMin;
                for (int y = 0; y < _ucHeight; y++)
                {
                    _ucData[x, y] = (float) GenerateCylindrical(ca, ch);
                    if (x >= _ucBorder && y >= _ucBorder && x < Width + _ucBorder &&
                        y < Height + _ucBorder)
                        _data[x - _ucBorder, y - _ucBorder] = (float) GenerateCylindrical(ca, ch);
                    // Cropped data
                    ch += yd;
                }

                ca += xd;
            }
        }

        /// <summary>
        ///     Generates a spherical projection of a point in the noise map.
        /// </summary>
        /// <param name="lat">The latitude of the point.</param>
        /// <param name="lon">The longitude of the point.</param>
        /// <returns>The corresponding noise map value.</returns>
        private double GenerateSpherical(double lat, double lon)
        {
            double r = Math.Cos(Mathf.Deg2Rad * lat);
            return Generator.GetValue(r * Math.Cos(Mathf.Deg2Rad * lon), Math.Sin(Mathf.Deg2Rad * lat),
                r * Math.Sin(Mathf.Deg2Rad * lon));
        }

        /// <summary>
        ///     Generates a spherical projection of the noise map.
        /// </summary>
        /// <param name="south">The clip region to the south.</param>
        /// <param name="north">The clip region to the north.</param>
        /// <param name="west">The clip region to the west.</param>
        /// <param name="east">The clip region to the east.</param>
        public void GenerateSpherical(double south, double north, double west, double east)
        {
            if (east <= west || north <= south)
                throw new ArgumentException("Invalid east/west or north/south combination");
            if (Generator == null)
                throw new ArgumentNullException("Generator is null");
            double loe = east - west;
            double lae = north - south;
            double xd = loe / ((double) Width - _ucBorder);
            double yd = lae / ((double) Height - _ucBorder);
            double clo = west;
            for (int x = 0; x < _ucWidth; x++)
            {
                double cla = south;
                for (int y = 0; y < _ucHeight; y++)
                {
                    _ucData[x, y] = (float) GenerateSpherical(cla, clo);
                    if (x >= _ucBorder && y >= _ucBorder && x < Width + _ucBorder &&
                        y < Height + _ucBorder)
                        _data[x - _ucBorder, y - _ucBorder] = (float) GenerateSpherical(cla, clo);
                    // Cropped data
                    cla += yd;
                }

                clo += xd;
            }
        }

        /// <summary>
        ///     Creates a grayscale texture map for the current content of the noise map.
        /// </summary>
        /// <returns>The created texture map.</returns>
        public Texture2D GetTexture()
        {
            return GetTexture(GradientPresets.Grayscale);
        }

        /// <summary>
        ///     Creates a texture map for the current content of the noise map.
        /// </summary>
        /// <param name="gradient">The gradient to color the texture map with.</param>
        /// <returns>The created texture map.</returns>
        public Texture2D GetTexture(Gradient gradient)
        {
            Texture2D texture = new Texture2D(Width, Height);
            Color[] pixels = new Color[Width * Height];
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                float sample;
                if (!float.IsNaN(Border) &&
                    (x == 0 || x == Width - _ucBorder || y == 0 || y == Height - _ucBorder))
                    sample = Border;
                else
                    sample = _data[x, y];
                pixels[x + y * Width] = gradient.Evaluate((sample + 1) / 2);
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }

        /// <summary>
        ///     Creates a normal map for the current content of the noise map.
        /// </summary>
        /// <param name="intensity">The scaling of the normal map values.</param>
        /// <returns>The created normal map.</returns>
        public Texture2D GetNormalMap(float intensity)
        {
            Texture2D texture = new Texture2D(Width, Height);
            Color[] pixels = new Color[Width * Height];
            for (int x = 0; x < _ucWidth; x++)
            for (int y = 0; y < _ucHeight; y++)
            {
                float xPos = (_ucData[Mathf.Max(0, x - _ucBorder), y] -
                              _ucData[Mathf.Min(x + _ucBorder, Height + _ucBorder), y]) / 2;
                float yPos = (_ucData[x, Mathf.Max(0, y - _ucBorder)] -
                              _ucData[x, Mathf.Min(y + _ucBorder, Width + _ucBorder)]) / 2;
                Vector3 normalX = new Vector3(xPos * intensity, 0, 1);
                Vector3 normalY = new Vector3(0, yPos * intensity, 1);
                // Get normal vector
                Vector3 normalVector = normalX + normalY;
                normalVector.Normalize();
                // Get color vector
                Vector3 colorVector = Vector3.zero;
                colorVector.x = (normalVector.x + 1) / 2;
                colorVector.y = (normalVector.y + 1) / 2;
                colorVector.z = (normalVector.z + 1) / 2;
                // Start at (x + _ucBorder, y + _ucBorder) so that resulting normal map aligns with cropped data
                if (x >= _ucBorder && y >= _ucBorder && x < Width + _ucBorder &&
                    y < Height + _ucBorder)
                    pixels[x - _ucBorder + (y - _ucBorder) * Width] = new Color(colorVector.x,
                        colorVector.y, colorVector.z);
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        ///     Gets a value whether the object is disposed.
        /// </summary>
        [field: XmlIgnore]
        [field: NonSerialized]
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///     Immediately releases the unmanaged resources used by this object.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
                IsDisposed = Disposing();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Immediately releases the unmanaged resources used by this object.
        /// </summary>
        /// <returns>True if the object is completely disposed.</returns>
        protected virtual bool Disposing()
        {
            _data = null;
            Width = 0;
            Height = 0;
            return true;
        }

        #endregion
    }
}