namespace GameLibrary.Util
{
    public enum Direction
    {
        // 6-connected
        Left, Right, Bottom, Top, Back, Front,
        // 18-connected
        BottomLeft, BottomRight, BottomFront, BottomBack,
        LeftFront, RightFront, LeftBack, RightBack,
        TopLeft, TopRight, TopFront, TopBack,
        // 26-connected
        BottomLeftFront, BottomRightFront, BottomLeftBack, BottomRightBack,
        TopLeftFront, TopRightFront, TopLeftBack, TopRightBack,
    }
}
