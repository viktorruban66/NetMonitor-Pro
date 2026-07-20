// netmonitor_java.java — монитор сети в реальном времени на Java (Swing)

import javax.swing.*;
import java.awt.*;
import java.awt.event.*;
import java.awt.geom.*;
import java.util.*;
import java.util.List;
import java.io.*;
import java.time.*;

public class NetMonitorJava extends JFrame {
    private static final int HISTORY_SIZE = 60;
    private Deque<Double> downHistory = new ArrayDeque<>(HISTORY_SIZE);
    private Deque<Double> upHistory = new ArrayDeque<>(HISTORY_SIZE);
    private Deque<Long> timeHistory = new ArrayDeque<>(HISTORY_SIZE);
    private double totalDown = 0, totalUp = 0;
    private double peakDown = 0, peakUp = 0;
    private Timer timer;
    private Random rand = new Random();

    private JLabel downLabel, upLabel, totalLabel;
    private GraphPanel graphPanel;

    public NetMonitorJava() {
        setTitle("📊 NetMonitor Pro — Java");
        setSize(800, 600);
        setDefaultCloseOperation(EXIT_ON_CLOSE);
        setLayout(new BorderLayout());

        // Info panel
        JPanel infoPanel = new JPanel(new FlowLayout());
        downLabel = new JLabel("⬇ Загрузка: 0.0 Мбит/с");
        upLabel = new JLabel("⬆ Выгрузка: 0.0 Мбит/с");
        totalLabel = new JLabel("Трафик: 0/0 МБ");
        infoPanel.add(downLabel);
        infoPanel.add(upLabel);
        infoPanel.add(totalLabel);
        add(infoPanel, BorderLayout.NORTH);

        // Graph
        graphPanel = new GraphPanel();
        add(graphPanel, BorderLayout.CENTER);

        // Buttons
        JPanel btnPanel = new JPanel();
        JButton exportBtn = new JButton("Экспорт CSV");
        JButton resetBtn = new JButton("Сброс статистики");
        btnPanel.add(exportBtn);
        btnPanel.add(resetBtn);
        add(btnPanel, BorderLayout.SOUTH);

        exportBtn.addActionListener(e -> exportCSV());
        resetBtn.addActionListener(e -> resetStats());

        // Timer
        timer = new Timer(1000, e -> updateData());
        timer.start();

        // Начальные данные
        for (int i = 0; i < 10; i++) {
            downHistory.add(0.0);
            upHistory.add(0.0);
            timeHistory.add(System.currentTimeMillis()/1000);
        }
    }

    private void updateData() {
        // Симуляция
        double down = rand.nextDouble() * 13 + 2;
        double up = rand.nextDouble() * 7 + 1;

        downHistory.addLast(down);
        upHistory.addLast(up);
        timeHistory.addLast(System.currentTimeMillis()/1000);
        if (downHistory.size() > HISTORY_SIZE) {
            downHistory.removeFirst();
            upHistory.removeFirst();
            timeHistory.removeFirst();
        }

        totalDown += down;
        totalUp += up;
        if (down > peakDown) peakDown = down;
        if (up > peakUp) peakUp = up;

        SwingUtilities.invokeLater(() -> {
            downLabel.setText(String.format("⬇ Загрузка: %.1f Мбит/с", down));
            upLabel.setText(String.format("⬆ Выгрузка: %.1f Мбит/с", up));
            totalLabel.setText(String.format("Трафик: %.1f/%.1f МБ", totalDown/8, totalUp/8));
            graphPanel.repaint();
        });
    }

    private void exportCSV() {
        if (downHistory.size() < 2) {
            JOptionPane.showMessageDialog(this, "Недостаточно данных для экспорта");
            return;
        }
        String filename = String.format("net_data_%d.csv", System.currentTimeMillis()/1000);
        try (PrintWriter pw = new PrintWriter(new File(filename))) {
            pw.println("Время,Загрузка(Мбит/с),Выгрузка(Мбит/с)");
            Iterator<Double> downIt = downHistory.iterator();
            Iterator<Double> upIt = upHistory.iterator();
            Iterator<Long> timeIt = timeHistory.iterator();
            while (downIt.hasNext()) {
                pw.printf("%d,%.2f,%.2f\n", timeIt.next(), downIt.next(), upIt.next());
            }
            JOptionPane.showMessageDialog(this, "Данные сохранены в " + filename);
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    private void resetStats() {
        downHistory.clear();
        upHistory.clear();
        timeHistory.clear();
        totalDown = 0;
        totalUp = 0;
        peakDown = 0;
        peakUp = 0;
        for (int i = 0; i < 10; i++) {
            downHistory.add(0.0);
            upHistory.add(0.0);
            timeHistory.add(System.currentTimeMillis()/1000);
        }
        graphPanel.repaint();
    }

    class GraphPanel extends JPanel {
        @Override
        protected void paintComponent(Graphics g) {
            super.paintComponent(g);
            Graphics2D g2 = (Graphics2D) g;
            g2.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);

            int w = getWidth();
            int h = getHeight();
            int margin = 40;

            // Оси
            g2.setColor(Color.BLACK);
            g2.drawLine(margin, margin, margin, h-margin);
            g2.drawLine(margin, h-margin, w-margin, h-margin);

            if (downHistory.size() < 2) return;

            int n = downHistory.size();
            double stepX = (double)(w - 2*margin) / (n-1);
            double maxY = 0;
            for (double d : downHistory) if (d > maxY) maxY = d;
            for (double d : upHistory) if (d > maxY) maxY = d;
            maxY = Math.max(maxY, 10);

            // Рисуем сетку
            g2.setColor(Color.LIGHT_GRAY);
            for (int i = 0; i <= 5; i++) {
                int y = h - margin - (int)(i * (h-2*margin) / 5);
                g2.drawLine(margin, y, w-margin, y);
                g2.drawString(String.valueOf((int)(i*maxY/5)), 5, y+4);
            }

            // График загрузки (синий)
            g2.setColor(Color.BLUE);
            double[] downArr = downHistory.stream().mapToDouble(Double::doubleValue).toArray();
            for (int i = 1; i < n; i++) {
                int x1 = margin + (int)((i-1) * stepX);
                int y1 = h - margin - (int)(downArr[i-1] / maxY * (h-2*margin));
                int x2 = margin + (int)(i * stepX);
                int y2 = h - margin - (int)(downArr[i] / maxY * (h-2*margin));
                g2.drawLine(x1, y1, x2, y2);
                g2.fillOval(x2-3, y2-3, 6, 6);
            }

            // График выгрузки (красный)
            g2.setColor(Color.RED);
            double[] upArr = upHistory.stream().mapToDouble(Double::doubleValue).toArray();
            for (int i = 1; i < n; i++) {
                int x1 = margin + (int)((i-1) * stepX);
                int y1 = h - margin - (int)(upArr[i-1] / maxY * (h-2*margin));
                int x2 = margin + (int)(i * stepX);
                int y2 = h - margin - (int)(upArr[i] / maxY * (h-2*margin));
                g2.drawLine(x1, y1, x2, y2);
                g2.fillOval(x2-3, y2-3, 6, 6);
            }

            // Легенда
            g2.setColor(Color.BLUE);
            g2.drawString("Загрузка", w-70, margin+20);
            g2.setColor(Color.RED);
            g2.drawString("Выгрузка", w-70, margin+40);
        }
    }

    public static void main(String[] args) throws Exception {
        UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
        SwingUtilities.invokeLater(() -> new NetMonitorJava().setVisible(true));
    }
}
