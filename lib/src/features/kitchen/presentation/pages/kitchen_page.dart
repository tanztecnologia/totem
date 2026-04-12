import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/entities/kitchen_order.dart';
import '../bloc/kitchen_cubit.dart';

class KitchenPage extends StatefulWidget {
  const KitchenPage({super.key});

  @override
  State<KitchenPage> createState() => _KitchenPageState();
}

class _KitchenPageState extends State<KitchenPage> {
  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    context.read<KitchenCubit>().loadOrders();
    _pollingTimer = Timer.periodic(const Duration(seconds: 15), (_) {
      context.read<KitchenCubit>().loadOrders();
    });
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      backgroundColor: colorScheme.surface,
      appBar: AppBar(
        title: const Text('Totem - Modo Cozinha'),
        backgroundColor: colorScheme.primaryContainer,
      ),
      body: BlocBuilder<KitchenCubit, KitchenState>(
        builder: (context, state) {
          if (state is KitchenLoading) {
            return const Center(child: CircularProgressIndicator());
          } else if (state is KitchenError) {
            return Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('Erro: ${state.message}', style: TextStyle(color: colorScheme.error)),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: () => context.read<KitchenCubit>().loadOrders(),
                    child: const Text('Tentar Novamente'),
                  ),
                ],
              ),
            );
          } else if (state is KitchenLoaded) {
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: _KitchenColumn(
                    title: 'Na Fila',
                    orders: state.queued,
                    color: Colors.grey.shade200,
                  ),
                ),
                Expanded(
                  child: _KitchenColumn(
                    title: 'Em Preparo',
                    orders: state.inPreparation,
                    color: Colors.orange.shade100,
                  ),
                ),
                Expanded(
                  child: _KitchenColumn(
                    title: 'Pronto',
                    orders: state.ready,
                    color: Colors.green.shade100,
                  ),
                ),
              ],
            );
          }
          return const SizedBox.shrink();
        },
      ),
    );
  }
}

class _KitchenColumn extends StatelessWidget {
  final String title;
  final List<KitchenOrder> orders;
  final Color color;

  const _KitchenColumn({
    required this.title,
    required this.orders,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;

    return Container(
      margin: const EdgeInsets.all(8),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                title,
                style: textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
              ),
              CircleAvatar(
                radius: 16,
                backgroundColor: Colors.white,
                child: Text(
                  '${orders.length}',
                  style: textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Expanded(
            child: ListView.separated(
              itemCount: orders.length,
              separatorBuilder: (context, index) => const SizedBox(height: 12),
              itemBuilder: (context, index) {
                final order = orders[index];
                return _KitchenOrderCard(order: order);
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _KitchenOrderCard extends StatelessWidget {
  final KitchenOrder order;

  const _KitchenOrderCard({required this.order});

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    final cubit = context.read<KitchenCubit>();
    final elapsedSeconds = order.currentStageElapsedSeconds > 0
        ? order.currentStageElapsedSeconds
        : DateTime.now().difference(order.createdAt).inSeconds;
    final targetSeconds = order.currentStageTargetSeconds;
    final timerStyle = _getTimerStyle(elapsedSeconds, targetSeconds, order.isOverdue);

    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Pedido: ${order.id.split("-").last.toUpperCase()}',
                  style: textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: timerStyle.background,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    _formatTimer(_getStageLabel(order.status), elapsedSeconds, targetSeconds),
                    style: textTheme.bodySmall?.copyWith(
                      color: timerStyle.foreground,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              'Local: ${order.fulfillment == 'DineIn' ? 'Comer no Local' : 'Para Levar'}',
              style: textTheme.bodyMedium?.copyWith(color: Colors.grey.shade700),
            ),
            const Divider(),
            ...order.items.map((item) => Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${item.quantity}x ',
                        style: textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.bold),
                      ),
                      Expanded(
                        child: Text(
                          item.name,
                          style: textTheme.bodyLarge,
                        ),
                      ),
                    ],
                  ),
                )),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () => cubit.advanceOrderStatus(order.id, order.status),
                style: ElevatedButton.styleFrom(
                  backgroundColor: _getActionColor(order.status),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 12),
                ),
                child: Text(_getActionText(order.status)),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Color _getActionColor(KitchenOrderStatus status) {
    switch (status) {
      case KitchenOrderStatus.queued:
        return Colors.orange;
      case KitchenOrderStatus.inPreparation:
        return Colors.green;
      case KitchenOrderStatus.ready:
        return Colors.blue;
      default:
        return Colors.grey;
    }
  }

  ({Color background, Color foreground}) _getTimerStyle(int elapsedSeconds, int targetSeconds, bool isOverdue) {
    if (targetSeconds <= 0) return (background: Colors.grey.shade200, foreground: Colors.grey.shade900);
    if (isOverdue) return (background: Colors.red.shade100, foreground: Colors.red.shade900);

    final warningAtSeconds = (targetSeconds * 3) ~/ 4;
    if (elapsedSeconds >= warningAtSeconds) return (background: Colors.orange.shade100, foreground: Colors.orange.shade900);
    return (background: Colors.grey.shade200, foreground: Colors.grey.shade900);
  }

  String _getStageLabel(KitchenOrderStatus status) {
    switch (status) {
      case KitchenOrderStatus.queued:
        return 'Espera';
      case KitchenOrderStatus.inPreparation:
        return 'Preparo';
      case KitchenOrderStatus.ready:
        return 'Pronto';
      default:
        return 'Tempo';
    }
  }

  String _formatTimer(String label, int elapsedSeconds, int targetSeconds) {
    final elapsedText = _formatMinutes(elapsedSeconds);
    if (targetSeconds <= 0) return '$label: $elapsedText';
    final targetText = _formatMinutes(targetSeconds);
    return '$label: $elapsedText / $targetText';
  }

  String _formatMinutes(int seconds) {
    final minutes = seconds ~/ 60;
    return '${minutes}m';
  }

  String _getActionText(KitchenOrderStatus status) {
    switch (status) {
      case KitchenOrderStatus.queued:
        return 'Iniciar Preparo';
      case KitchenOrderStatus.inPreparation:
        return 'Marcar como Pronto';
      case KitchenOrderStatus.ready:
        return 'Entregar Pedido';
      default:
        return 'Ação Indisponível';
    }
  }
}
