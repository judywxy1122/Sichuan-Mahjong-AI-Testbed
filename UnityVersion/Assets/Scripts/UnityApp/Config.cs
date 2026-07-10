namespace SichuanMahjong.UnityApp
{
    /// <summary>
    /// Port of application.config.Config — all layout math is in the original
    /// 1920x960 logical pixel space; GameRoot scales it to the window.
    /// </summary>
    public static class Config
    {
        // Settings
        public const int FPS = 60;
        public const int LOG_ITEMS = 39;
        public const bool USE_PHOTO_BACKGROUND = true;
        public const string BACKGROUND_IMAGE_PATH = "img/background/star_sky_over_sea_01.jpg";

        // Frame
        public const int ORIGINAL_TILE_SIZE = 16;
        private const int TILE_SIZE = ORIGINAL_TILE_SIZE * 4;
        public const int SCREEN_COLUMNS = 30;
        public const int SCREEN_ROWS = 15;
        public const int SCREEN_WIDTH = TILE_SIZE * SCREEN_COLUMNS;   // 1920
        public const int SCREEN_HEIGHT = TILE_SIZE * SCREEN_ROWS;     // 960

        // Tile sizes
        private const double TILE_HEIGHT_MULTIPLIER = 1.4;
        public const int TILE_WIDTH = TILE_SIZE;                       // 64
        public const int TILE_HEIGHT = (int)(TILE_WIDTH * TILE_HEIGHT_MULTIPLIER); // 89

        // Player hand
        public const int PLAYER_HAND_X = 200;
        public const int PLAYER_HAND_Y = 600;
        public const int PLAYER_HAND_WIDTH = 1100;
        public const int PLAYER_HAND_HEIGHT = 200;
        public const int PLAYER_HAND_TOP_INDENT = PLAYER_HAND_Y + 50;
        public const int FOURTEENTH_TILE_INDENT = 30;
        public const int PLAYER_HAND_TILE_PADDING = 10;

        // Table commons
        private const double SHRINK_MULTIPLIER = 0.65;
        public const int TABLE_TILE_PADDING = (int)(PLAYER_HAND_TILE_PADDING * SHRINK_MULTIPLIER); // 6
        public const int TABLE_TILE_WIDTH = (int)(TILE_WIDTH * SHRINK_MULTIPLIER);                 // 41
        public const int TABLE_TILE_HEIGHT = (int)(TILE_HEIGHT * SHRINK_MULTIPLIER);               // 57

        // Player table
        public const int PLAYER_TABLE_X = 200;
        public const int PLAYER_TABLE_Y = 500;
        public const int PLAYER_TABLE_WIDTH = PLAYER_HAND_WIDTH;
        public const int PLAYER_TABLE_HEIGHT = 100;

        // AI tables
        public const int AI_TABLE_PADDING = 200;
        public const int AI_TABLE_Y = 145;
        public const int AI_TABLE_WIDTH = 250;
        public const int AI_TABLE_HEIGHT = 260;
        public const int AI_TABLE_NUM_TILES_PER_LINE = 5;

        public const int AI1_TABLE_X = 100;
        public const int AI1_TABLE_Y = AI_TABLE_Y;
        public const int AI1_TABLE_WIDTH = AI_TABLE_WIDTH;
        public const int AI1_TABLE_HEIGHT = AI_TABLE_HEIGHT;

        public const int AI2_TABLE_X = AI1_TABLE_X + AI1_TABLE_WIDTH + AI_TABLE_PADDING;
        public const int AI2_TABLE_Y = AI_TABLE_Y;
        public const int AI2_TABLE_WIDTH = AI_TABLE_WIDTH;
        public const int AI2_TABLE_HEIGHT = AI_TABLE_HEIGHT;

        public const int AI3_TABLE_X = AI2_TABLE_X + AI2_TABLE_WIDTH + AI_TABLE_PADDING;
        public const int AI3_TABLE_Y = AI_TABLE_Y;
        public const int AI3_TABLE_WIDTH = AI_TABLE_WIDTH;
        public const int AI3_TABLE_HEIGHT = AI_TABLE_HEIGHT;
    }
}
