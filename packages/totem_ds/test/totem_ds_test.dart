import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:totem_ds/totem_ds.dart';

void main() {
  testWidgets('TotemProductCard renderiza', (WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: TotemProductCard(
          title: 'Hambúrguer',
          priceText: 'R\$ 19,90',
          badgeCount: 1,
          onBuy: () {},
        ),
      ),
    );

    expect(find.text('Hambúrguer'), findsOneWidget);
    expect(find.text('R\$ 19,90'), findsOneWidget);
    expect(find.text('1'), findsOneWidget);
  });
}
