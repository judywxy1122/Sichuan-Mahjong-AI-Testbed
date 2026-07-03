package application.gameplay;

import model.basic.Tile;
import model.basic.TileTypeEnum;

public final class TileCodec {
    private TileCodec() {
    }

    public static String code(Tile tile) {
        return tile == null ? "" : tile.toString();
    }

    public static Tile fromCode(String code) {
        if (code == null || code.length() < 2) {
            return null;
        }
        try {
            TileTypeEnum type = TileTypeEnum.valueOf(code.substring(0, 1));
            int number = Integer.parseInt(code.substring(1));
            if (number < 1 || number > 9) {
                return null;
            }
            return new Tile(type, number);
        } catch (IllegalArgumentException e) {
            return null;
        }
    }

    public static String display(Tile tile) {
        if (tile == null) {
            return "";
        }
        String[] chineseNumbers = {"", "一", "二", "三", "四", "五", "六", "七", "八", "九"};
        if (tile.getType() == TileTypeEnum.B && tile.getNumber() == 1) {
            return "幺鸡";
        }
        return chineseNumbers[tile.getNumber()] + tile.getType().getChinese();
    }
}
