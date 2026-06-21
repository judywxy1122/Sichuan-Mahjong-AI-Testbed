import application.gameframe.GamePanel;
import com.github.esrrhs.majiang_algorithm.AITable;
import com.github.esrrhs.majiang_algorithm.AITableFeng;
import com.github.esrrhs.majiang_algorithm.AITableJian;

import javax.swing.*;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;

public class Main {
    public static void main(String[] args) throws IOException {

        Path probabilityDir = Paths.get("probability");
        AITableJian.load(Files.readAllLines(probabilityDir.resolve("majiang_ai_jian.txt")));
        AITableFeng.load(Files.readAllLines(probabilityDir.resolve("majiang_ai_feng.txt")));
        AITable.load(Files.readAllLines(probabilityDir.resolve("majiang_ai_normal.txt")));
        JFrame window = new JFrame("Mahjong");
        window.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        window.setResizable(true);

        GamePanel gamePanel = new GamePanel();

        window.add(gamePanel);
        window.pack();
        window.setLocationRelativeTo(null);
        window.setVisible(true);
        window.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        gamePanel.start();
    }

}
