package application.gameframe;

import application.game.Game;
import model.GameState;
import application.config.Config;
import model.players.Player;
import model.basic.Tile;
import model.players.PlayerStatusEnum;
import model.tiles.Group;

import javax.swing.*;

import java.io.IOException;
import java.awt.*;
import java.awt.event.*;
import java.net.URI;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Random;

import static utils.TileUtils.*;

public class GamePanel extends JPanel implements Runnable {

    Thread gameThread;
    KeyHandler keyHandler = new KeyHandler();
    private Game game;
    private Player player;
    private Tile hoveredTile = null;
    private final TileImageLoader imageLoader = new TileImageLoader();
    private List<Tile> interactableTiles = new ArrayList<>();
    private Rectangle winningHandButtonBounds = new Rectangle();
    private Map<String, Rectangle> actionButtonBounds = new LinkedHashMap<>();
    private Map<String, Rectangle> endGameButtonBounds = new LinkedHashMap<>();
    private final List<String> rewardUrls = Collections.unmodifiableList(Arrays.asList(
            "https://www.tiktok.com/@innahbee/video/7527297056680070408",
            "https://www.tiktok.com/@innahbee/video/7516930359687269650",
            "https://www.tiktok.com/@innahbee/video/7519519217574595848",
            "https://www.tiktok.com/@innahbee/video/7333543909315874053",
            "https://www.tiktok.com/@innahbee/video/7519162164683361554",
            "https://www.tiktok.com/@innahbee/video/7379244215131213061",
            "https://www.tiktok.com/@innahbee/video/7416723958856158471",
            "https://www.tiktok.com/@innahbee/video/7299467527459966214"
    ));
    private final Random rewardRandom = new Random();

    public GamePanel() {
        this.game = new Game();
        this.player = this.game.getPlayers().get(0);

        this.setPreferredSize(getInitialPanelSize());
        this.setMinimumSize(new Dimension(640, 360));
        this.setBackground(Color.BLACK);
        this.setDoubleBuffered(true);
        this.addKeyListener(keyHandler);
        this.setFocusable(true);

        addMouseListener(new MouseAdapter() {
            @Override
            public void mousePressed(MouseEvent e) {
                Point logicalPoint = toLogicalPoint(e.getPoint());
                if (logicalPoint == null) {
                    return;
                }
                String endGameAction = getClickedEndGameAction(logicalPoint);
                if (endGameAction != null) {
                    processEndGameButton(endGameAction);
                    return;
                }
                if (game.getWinner() != null && winningHandButtonBounds.contains(logicalPoint)) {
                    showWinningHandDialog();
                    return;
                }
                String action = getClickedAction(logicalPoint);
                if (action != null) {
                    processActionButton(action);
                    return;
                }
                if (hoveredTile == null) {
                    return;
                }
                if (player.isPlaying() && !player.containsChouPungKong()) {
                    player.plays(hoveredTile);
                    hoveredTile = null;
                    game.processPlayed();
                } else if (!player.isPlaying() && player.containsResponseAction()) {
                    game.showInvalidInput("Response phase: use H/C/P/K to respond, or S to skip.");
                } else {
                    game.showInvalidInput("It is not your discard turn yet.");
                }
            }
        });

        addMouseMotionListener(new MouseMotionAdapter() {
            @Override
            public void mouseMoved(MouseEvent e) {
                Point logicalPoint = toLogicalPoint(e.getPoint());
                if (logicalPoint == null) {
                    hoveredTile = null;
                    return;
                }
                if (getClickedAction(logicalPoint) != null
                        || getClickedEndGameAction(logicalPoint) != null
                        || (game.getWinner() != null && winningHandButtonBounds.contains(logicalPoint))) {
                    hoveredTile = null;
                    return;
                }
                List<Tile> hand = new ArrayList<Tile>() {{
                    this.addAll(interactableTiles);
                }};
                Tile newHoveredTile = getTileAt(logicalPoint.x, logicalPoint.y, hand);
                if (newHoveredTile != hoveredTile) {
                    hoveredTile = newHoveredTile;
                }
            }
        });
    }

    public void start() {
        if (gameThread == null) {
            gameThread = new Thread(this);
            gameThread.start();
        }
    }

    public void update() {
        if (this.game.isOver()) {
            this.gameThread = null;
        }
        if (keyHandler.hPressed && !keyHandler.hProcessed
                && player.getStatus().contains(PlayerStatusEnum.HU)) {
            this.game.processHu(player);
            keyHandler.hProcessed = true;
        }
        if (keyHandler.cPressed && !keyHandler.cProcessed
                && player.containsChow()) {
            this.game.processChou(player);
            keyHandler.cProcessed = true;
        }
        if (keyHandler.pPressed && !keyHandler.pProcessed
                && player.containsPung()) {
            this.game.processPung(player);
            keyHandler.pProcessed = true;
        }
        if (keyHandler.kPressed && !keyHandler.kProcessed
                && player.containsKong()) {
            this.game.processKong(player);
            keyHandler.kProcessed = true;
        }
        if (keyHandler.sPressed && !keyHandler.sProcessed
                && ((!player.isPlaying() && player.containsResponseAction()) || player.containsChouPungKong())) {
            this.game.processSkip(player);
            keyHandler.sProcessed = true;
        }
    }

    @Override
    public void paintComponent(Graphics g) {
        super.paintComponent(g);
        Graphics2D g2 = (Graphics2D) g.create();
        g2.scale(getRenderScaleX(), getRenderScaleY());

        Drawer drawer = new Drawer(g2, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT, imageLoader);
        drawer.drawBackground();
        drawer.drawLogs(this.game.getLog().getLastXMessages(Config.LOG_ITEMS));
        actionButtonBounds = buildActionButtonBounds();
        endGameButtonBounds = buildEndGameButtonBounds();
        winningHandButtonBounds = buildWinningHandButtonBounds();
        drawer.drawHelperBoxes(this.game.getTurnPlayer(), this.game.getLastActionPlayer(),
                this.game.getLastActionText(), this.game.getStatusText(),
                this.game.getWinner(), winningHandButtonBounds, actionButtonBounds, endGameButtonBounds);

        for (Player p : game.getPlayers()) {
            drawer.drawTable(p.getTable().toList(), p.getPosition());
            drawer.drawPungKong(p.getHand().getPungKong(), p.getPosition());
        }
        interactableTiles = drawer.drawPlayerHand(player.getHand().toList(), player.getHand().getNewTile());
        if (hoveredTile != null) {
            drawer.drawTile(hoveredTile, Color.LIGHT_GRAY);
        }
        g2.dispose();
    }

    private Dimension getInitialPanelSize() {
        Rectangle available = GraphicsEnvironment.getLocalGraphicsEnvironment().getMaximumWindowBounds();
        int usableWidth = Math.max(640, available.width - 80);
        int usableHeight = Math.max(360, available.height - 120);
        double scale = Math.min(1.0, Math.min(
                usableWidth / (double) Config.SCREEN_WIDTH,
                usableHeight / (double) Config.SCREEN_HEIGHT));
        return new Dimension((int) (Config.SCREEN_WIDTH * scale), (int) (Config.SCREEN_HEIGHT * scale));
    }

    private double getRenderScaleX() {
        return getWidth() / (double) Config.SCREEN_WIDTH;
    }

    private double getRenderScaleY() {
        return getHeight() / (double) Config.SCREEN_HEIGHT;
    }

    private Point toLogicalPoint(Point screenPoint) {
        double scaleX = getRenderScaleX();
        double scaleY = getRenderScaleY();
        if (scaleX <= 0 || scaleY <= 0) {
            return null;
        }
        int x = (int) (screenPoint.x / scaleX);
        int y = (int) (screenPoint.y / scaleY);
        if (x < 0 || y < 0 || x > Config.SCREEN_WIDTH || y > Config.SCREEN_HEIGHT) {
            return null;
        }
        return new Point(x, y);
    }

    private Map<String, Rectangle> buildActionButtonBounds() {
        Map<String, Rectangle> buttons = new LinkedHashMap<>();
        if (game.getWinner() != null) {
            return buttons;
        }

        List<String> actions = new ArrayList<>();
        if (player.getStatus().contains(PlayerStatusEnum.HU)) {
            actions.add("Hu");
        }
        if (player.containsChow()) {
            actions.add("Chow");
        }
        if (player.containsPung()) {
            actions.add("Pung");
        }
        if (player.containsKong()) {
            actions.add("Kong");
        }
        if (!actions.isEmpty() && (!player.isPlaying() || player.containsChouPungKong())) {
            actions.add("Skip");
        }

        int buttonWidth = 92;
        int buttonHeight = 32;
        int gap = 10;
        int totalWidth = actions.size() * buttonWidth + Math.max(0, actions.size() - 1) * gap;
        int startX = Config.SCREEN_WIDTH - totalWidth - 64;
        int y = 78;
        for (int i = 0; i < actions.size(); i++) {
            buttons.put(actions.get(i), new Rectangle(startX + i * (buttonWidth + gap), y, buttonWidth, buttonHeight));
        }
        return buttons;
    }

    private Map<String, Rectangle> buildEndGameButtonBounds() {
        Map<String, Rectangle> buttons = new LinkedHashMap<>();
        if (!game.isOver()) {
            return buttons;
        }

        int buttonHeight = 32;
        int gap = 10;
        int y = 78;
        if (game.getWinner() != null) {
            List<String> labels = new ArrayList<>();
            if (player.equals(game.getWinner())) {
                labels.add("Reward");
            }
            labels.add("New game");
            labels.add("Exit");

            int totalWidth = 0;
            for (String label : labels) {
                totalWidth += getEndGameButtonWidth(label);
            }
            totalWidth += Math.max(0, labels.size() - 1) * gap;

            int x = buildWinningHandButtonBounds().x - gap - totalWidth;
            for (String label : labels) {
                int buttonWidth = getEndGameButtonWidth(label);
                buttons.put(label, new Rectangle(x, y, buttonWidth, buttonHeight));
                x += buttonWidth + gap;
            }
        } else {
            buttons.put("New game", new Rectangle(Config.SCREEN_WIDTH - 260, y, 125, buttonHeight));
            buttons.put("Exit", new Rectangle(Config.SCREEN_WIDTH - 125, y, 80, buttonHeight));
        }
        return buttons;
    }

    private int getEndGameButtonWidth(String label) {
        if ("Exit".equals(label)) {
            return 80;
        }
        return 105;
    }

    private Rectangle buildWinningHandButtonBounds() {
        if (game.getWinner() == null) {
            return new Rectangle();
        }
        return new Rectangle(Config.SCREEN_WIDTH - 260, 78, 205, 32);
    }

    private String getClickedAction(Point logicalPoint) {
        for (Map.Entry<String, Rectangle> entry : actionButtonBounds.entrySet()) {
            if (entry.getValue().contains(logicalPoint)) {
                return entry.getKey();
            }
        }
        return null;
    }

    private String getClickedEndGameAction(Point logicalPoint) {
        for (Map.Entry<String, Rectangle> entry : endGameButtonBounds.entrySet()) {
            if (entry.getValue().contains(logicalPoint)) {
                return entry.getKey();
            }
        }
        return null;
    }

    private void processActionButton(String action) {
        switch (action) {
            case "Hu":
                if (player.getStatus().contains(PlayerStatusEnum.HU)) {
                    game.processHu(player);
                }
                break;
            case "Chow":
                if (player.containsChow()) {
                    game.processChou(player);
                }
                break;
            case "Pung":
                if (player.containsPung()) {
                    game.processPung(player);
                }
                break;
            case "Kong":
                if (player.containsKong()) {
                    game.processKong(player);
                }
                break;
            case "Skip":
                if ((!player.isPlaying() && player.containsResponseAction()) || player.containsChouPungKong()) {
                    game.processSkip(player);
                }
                break;
            default:
                break;
        }
        requestFocusInWindow();
    }

    private void processEndGameButton(String action) {
        if ("Reward".equals(action)) {
            openReward();
        } else if ("New game".equals(action)) {
            startNewGame();
        } else if ("Exit".equals(action)) {
            Window window = SwingUtilities.getWindowAncestor(this);
            if (window != null) {
                window.dispose();
            }
            System.exit(0);
        }
    }

    private void openReward() {
        if (!player.equals(game.getWinner())) {
            game.showInvalidInput("Reward is available only when you win.");
            repaint();
            return;
        }
        if (!Desktop.isDesktopSupported() || !Desktop.getDesktop().isSupported(Desktop.Action.BROWSE)) {
            showRewardError("Cannot open browser from this desktop environment.");
            return;
        }

        String url = rewardUrls.get(rewardRandom.nextInt(rewardUrls.size()));
        try {
            Desktop.getDesktop().browse(URI.create(url));
        } catch (IOException | IllegalArgumentException | SecurityException e) {
            showRewardError("Could not open reward video: " + e.getMessage());
        }
        requestFocusInWindow();
    }

    private void showRewardError(String message) {
        JOptionPane.showMessageDialog(this, message, "Reward unavailable", JOptionPane.WARNING_MESSAGE);
        requestFocusInWindow();
    }

    private void startNewGame() {
        this.game = new Game();
        this.player = this.game.getPlayers().get(0);
        this.hoveredTile = null;
        this.interactableTiles = new ArrayList<>();
        this.actionButtonBounds = new LinkedHashMap<>();
        this.endGameButtonBounds = new LinkedHashMap<>();
        resetKeyState();
        start();
        repaint();
        requestFocusInWindow();
    }

    private void resetKeyState() {
        keyHandler.hPressed = false;
        keyHandler.cPressed = false;
        keyHandler.pPressed = false;
        keyHandler.kPressed = false;
        keyHandler.sPressed = false;
        keyHandler.hProcessed = false;
        keyHandler.cProcessed = false;
        keyHandler.pProcessed = false;
        keyHandler.kProcessed = false;
        keyHandler.sProcessed = false;
    }

    private void showWinningHandDialog() {
        Player winner = game.getWinner();
        if (winner == null) {
            return;
        }

        JDialog dialog = new JDialog(SwingUtilities.getWindowAncestor(this),
                winner.getName() + " winning hand", Dialog.ModalityType.MODELESS);
        dialog.setDefaultCloseOperation(WindowConstants.DISPOSE_ON_CLOSE);

        JPanel content = new JPanel(new BorderLayout(12, 12));
        content.setBorder(BorderFactory.createEmptyBorder(14, 16, 14, 16));
        content.setBackground(new Color(35, 70, 45));

        JLabel title = new JLabel(winner.getName() + " wins");
        title.setForeground(new Color(255, 245, 120));
        title.setFont(new Font("Arial", Font.BOLD, 20));
        content.add(title, BorderLayout.NORTH);

        JPanel tileRow = new JPanel(new FlowLayout(FlowLayout.LEFT, 8, 4));
        tileRow.setBackground(new Color(35, 70, 45));

        addSectionLabel(tileRow, "Winning shape");
        List<List<Tile>> concealedGroups = arrangeConcealedWinningGroups(winner, game.getWinningTile());
        if (concealedGroups.isEmpty()) {
            addTileGroup(tileRow, winner.getHand().toList());
            if (game.getWinningTile() != null) {
                addTileGroup(tileRow, new ArrayList<Tile>() {{
                    add(game.getWinningTile());
                }});
            }
        } else {
            for (List<Tile> group : concealedGroups) {
                addTileGroup(tileRow, group);
            }
        }

        List<Group> melds = winner.getHand().getPungKong();
        if (!melds.isEmpty()) {
            addSectionLabel(tileRow, "Melds");
            for (Group group : melds) {
                addTileGroup(tileRow, group.toList());
            }
        }

        JScrollPane scrollPane = new JScrollPane(tileRow,
                ScrollPaneConstants.VERTICAL_SCROLLBAR_NEVER,
                ScrollPaneConstants.HORIZONTAL_SCROLLBAR_AS_NEEDED);
        scrollPane.setBorder(BorderFactory.createEmptyBorder());
        content.add(scrollPane, BorderLayout.CENTER);

        dialog.setContentPane(content);
        dialog.setSize(980, 190);
        dialog.setLocationRelativeTo(this);
        dialog.setVisible(true);
    }

    private void addSectionLabel(JPanel panel, String text) {
        JLabel label = new JLabel(text);
        label.setForeground(Color.WHITE);
        label.setFont(new Font("Arial", Font.BOLD, 15));
        label.setBorder(BorderFactory.createEmptyBorder(0, 10, 0, 2));
        panel.add(label);
    }

    private void addTileLabel(JPanel panel, Tile tile) {
        JLabel label = new JLabel(new ImageIcon(imageLoader.getImage(tile)));
        label.setBorder(BorderFactory.createLineBorder(Color.WHITE, 1));
        panel.add(label);
    }

    private void addTileGroup(JPanel panel, List<Tile> tiles) {
        JPanel groupPanel = new JPanel(new FlowLayout(FlowLayout.LEFT, 2, 2));
        groupPanel.setBackground(new Color(45, 86, 56));
        groupPanel.setBorder(BorderFactory.createCompoundBorder(
                BorderFactory.createLineBorder(new Color(255, 245, 120), 1),
                BorderFactory.createEmptyBorder(4, 4, 4, 4)));
        for (Tile tile : tiles) {
            addTileLabel(groupPanel, tile);
        }
        panel.add(groupPanel);
    }

    private List<List<Tile>> arrangeConcealedWinningGroups(Player winner, Tile winningTile) {
        List<Tile> tiles = winner.getHand().toList();
        if (winner.getHand().getNewTile() != null) {
            tiles.add(winner.getHand().getNewTile());
        } else if (winningTile != null) {
            tiles.add(winningTile);
        }
        tiles.sort((a, b) -> {
            if (a.getType().equals(b.getType())) {
                return a.getNumber() - b.getNumber();
            }
            return a.getType().compareTo(b.getType());
        });

        List<List<Tile>> sevenPairs = tryArrangeSevenPairs(tiles);
        if (!sevenPairs.isEmpty()) {
            return sevenPairs;
        }

        int meldCount = winner.getHand().getPungKong().size();
        int groupsNeeded = 4 - meldCount;
        return tryArrangeStandardHu(tiles, groupsNeeded);
    }

    private List<List<Tile>> tryArrangeSevenPairs(List<Tile> tiles) {
        if (tiles.size() != 14) {
            return new ArrayList<>();
        }
        List<Tile> remaining = new ArrayList<>(tiles);
        List<List<Tile>> result = new ArrayList<>();
        while (!remaining.isEmpty()) {
            Tile first = remaining.remove(0);
            int matchIndex = findMatchingTileIndex(remaining, first);
            if (matchIndex < 0) {
                return new ArrayList<>();
            }
            List<Tile> pair = new ArrayList<>();
            pair.add(first);
            pair.add(remaining.remove(matchIndex));
            result.add(pair);
        }
        return result;
    }

    private List<List<Tile>> tryArrangeStandardHu(List<Tile> tiles, int groupsNeeded) {
        for (int i = 0; i < tiles.size(); i++) {
            Tile pairTile = tiles.get(i);
            int pairIndex = findMatchingTileIndex(tiles, pairTile, i + 1);
            if (pairIndex < 0) {
                continue;
            }

            List<Tile> remaining = new ArrayList<>(tiles);
            Tile secondPairTile = remaining.remove(pairIndex);
            Tile firstPairTile = remaining.remove(i);
            List<List<Tile>> groups = new ArrayList<>();
            if (arrangeGroups(remaining, groupsNeeded, groups)) {
                groups.add(new ArrayList<Tile>() {{
                    add(firstPairTile);
                    add(secondPairTile);
                }});
                return groups;
            }
        }
        return new ArrayList<>();
    }

    private boolean arrangeGroups(List<Tile> remaining, int groupsNeeded, List<List<Tile>> groups) {
        if (groupsNeeded == 0) {
            return remaining.isEmpty();
        }
        if (remaining.size() < 3) {
            return false;
        }

        remaining.sort((a, b) -> {
            if (a.getType().equals(b.getType())) {
                return a.getNumber() - b.getNumber();
            }
            return a.getType().compareTo(b.getType());
        });
        Tile first = remaining.get(0);

        int secondSame = findMatchingTileIndex(remaining, first, 1);
        if (secondSame >= 0) {
            int thirdSame = findMatchingTileIndex(remaining, first, secondSame + 1);
            if (thirdSame >= 0) {
                List<Tile> nextRemaining = new ArrayList<>(remaining);
                Tile third = nextRemaining.remove(thirdSame);
                Tile second = nextRemaining.remove(secondSame);
                Tile firstTile = nextRemaining.remove(0);
                groups.add(new ArrayList<Tile>() {{
                    add(firstTile);
                    add(second);
                    add(third);
                }});
                if (arrangeGroups(nextRemaining, groupsNeeded - 1, groups)) {
                    return true;
                }
                groups.remove(groups.size() - 1);
            }
        }

        Tile secondInSequence = new Tile(first.getType(), first.getNumber() + 1);
        Tile thirdInSequence = new Tile(first.getType(), first.getNumber() + 2);
        int secondIndex = findMatchingTileIndex(remaining, secondInSequence, 1);
        int thirdIndex = findMatchingTileIndex(remaining, thirdInSequence, 1);
        if (secondIndex >= 0 && thirdIndex >= 0) {
            List<Tile> nextRemaining = new ArrayList<>(remaining);
            Tile third = nextRemaining.remove(Math.max(secondIndex, thirdIndex));
            Tile second = nextRemaining.remove(Math.min(secondIndex, thirdIndex));
            Tile firstTile = nextRemaining.remove(0);
            groups.add(new ArrayList<Tile>() {{
                add(firstTile);
                add(second);
                add(third);
            }});
            if (arrangeGroups(nextRemaining, groupsNeeded - 1, groups)) {
                return true;
            }
            groups.remove(groups.size() - 1);
        }

        return false;
    }

    private int findMatchingTileIndex(List<Tile> tiles, Tile tile) {
        return findMatchingTileIndex(tiles, tile, 0);
    }

    private int findMatchingTileIndex(List<Tile> tiles, Tile tile, int startIndex) {
        for (int i = startIndex; i < tiles.size(); i++) {
            if (tiles.get(i).equals(tile)) {
                return i;
            }
        }
        return -1;
    }

    @Override
    public void run() {
        double interval = 1000000000.0 / Config.FPS;
        double nextTime = System.nanoTime() + interval;

        while (gameThread != null) {
            update();
            repaint();
            try {
                double wait = (nextTime - System.nanoTime()) / 1000000;
                if (wait < 0) {
                    wait = 0;
                }
                Thread.sleep((long) wait);
                nextTime += interval;
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        }
    }
}
