#!/usr/bin/env python3
"""Tests for extract_claims.py CLI."""

import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest

SCRIPT = os.path.join(os.path.dirname(__file__), '..', 'scripts', 'extract_claims.py')
FIXTURES = os.path.join(os.path.dirname(__file__), 'fixtures')


def run_ec(*args: str) -> dict | list:
    """Run extract_claims.py with args."""
    result = subprocess.run(
        [sys.executable, SCRIPT, *args],
        capture_output=True, text=True,
    )
    if result.returncode != 0:
        raise RuntimeError(f'Exit {result.returncode}: {result.stderr}')
    return json.loads(result.stdout)


SAMPLE_REPORT = """\
---
title: Test Research Report
---

## Executive Summary

This report examines the impact of quantum computing on cryptography [1, 2]. The field has advanced significantly since 2020, with major breakthroughs in error correction.

## Introduction

Quantum computing represents a paradigm shift in computational capability. Researchers at Google demonstrated quantum supremacy in 2019 using a 53-qubit processor [3]. This milestone confirmed theoretical predictions made decades earlier.

## Finding 1

The Shor algorithm can factor large numbers exponentially faster than classical methods [4]. Current RSA-2048 encryption could be broken by a sufficiently large quantum computer. However, such machines are estimated to require millions of physical qubits [5, 6].

## Finding 2

Post-quantum cryptography standards should be adopted within the next 5 years. Organizations should consider hybrid classical-quantum approaches during the transition period. NIST has already standardized several lattice-based algorithms [7].

## Synthesis

Taken together, the evidence suggests that quantum computing poses a real but manageable threat to current cryptographic systems. The timeline for practical quantum attacks remains uncertain, but proactive migration reduces risk substantially.

## Recommendations

Organizations should begin evaluating post-quantum cryptography solutions immediately. Security teams should conduct a cryptographic inventory to identify vulnerable systems. Companies should consider implementing crypto-agility frameworks to enable rapid algorithm switching.

## Bibliography

[1] Smith et al. (2023). Quantum Computing Advances.
[2] Johnson (2024). Cryptographic Implications.
"""


class TestExtract(unittest.TestCase):
    def setUp(self):
        self.tmpdir = tempfile.mkdtemp()
        # Create empty claims.jsonl
        open(os.path.join(self.tmpdir, 'claims.jsonl'), 'w').close()
        # Write sample report
        self.report_path = os.path.join(self.tmpdir, 'report.md')
        with open(self.report_path, 'w') as f:
            f.write(SAMPLE_REPORT)

    def tearDown(self):
        shutil.rmtree(self.tmpdir, ignore_errors=True)

    def test_extract_finds_claims(self):
        out = run_ec('extract', '--report', self.report_path, '--dir', self.tmpdir)
        self.assertEqual(out['status'], 'ok')
        self.assertGreater(out['claims_added'], 5)

    def test_extract_idempotent(self):
        out1 = run_ec('extract', '--report', self.report_path, '--dir', self.tmpdir)
        out2 = run_ec('extract', '--report', self.report_path, '--dir', self.tmpdir)
        self.assertEqual(out2['claims_added'], 0)
        self.assertEqual(out2['claims_skipped'], out1['claims_added'])

    def test_claim_types_assigned(self):
        run_ec('extract', '--report', self.report_path, '--dir', self.tmpdir)
        out = run_ec('stats', '--dir', self.tmpdir)
        # Should have at least factual and recommendation types
        self.assertIn('factual', out['by_type'])
        self.assertIn('recommendation', out['by_type'])

    def test_sections_detected(self):
        run_ec('extract', '--report', self.report_path, '--dir', self.tmpdir)
        out = run_ec('stats', '--dir', self.tmpdir)
        self.assertIn('finding_1', out['by_section'])
        self.assertIn('finding_2', out['by_section'])
        self.assertIn('recommendations', out['by_section'])


class TestAdd(unittest.TestCase):
    def setUp(self):
        self.tmpdir = tempfile.mkdtemp()
        open(os.path.join(self.tmpdir, 'claims.jsonl'), 'w').close()

    def tearDown(self):
        shutil.rmtree(self.tmpdir, ignore_errors=True)

    def test_add_and_dedup(self):
        claim = json.dumps({
            'section_id': 'finding_1',
            'text': 'Quantum computers can break RSA encryption.',
            'claim_type': 'factual',
        })
        out1 = run_ec('add', '--json', claim, '--dir', self.tmpdir)
        self.assertEqual(out1['status'], 'added')
        self.assertEqual(len(out1['claim_id']), 16)

        out2 = run_ec('add', '--json', claim, '--dir', self.tmpdir)
        self.assertEqual(out2['status'], 'duplicate')

    def test_add_with_sources(self):
        claim = json.dumps({
            'section_id': 'finding_1',
            'text': 'NIST standardized CRYSTALS-Kyber in 2024.',
            'claim_type': 'factual',
            'cited_source_ids': ['abcdef0123456789'],
            'evidence_ids': ['1234567890abcdef'],
        })
        out = run_ec('add', '--json', claim, '--dir', self.tmpdir)
        self.assertEqual(out['status'], 'added')


class TestListAndStats(unittest.TestCase):
    def setUp(self):
        self.tmpdir = tempfile.mkdtemp()
        open(os.path.join(self.tmpdir, 'claims.jsonl'), 'w').close()
        # Add mixed claims
        for sec, text, ctype in [
            ('finding_1', 'The sky appears blue due to Rayleigh scattering.', 'factual'),
            ('finding_1', 'Light wavelengths scatter differently in the atmosphere.', 'factual'),
            ('synthesis', 'Overall, atmospheric optics explains most visual phenomena.', 'synthesis'),
            ('recommendations', 'Researchers should investigate polarization effects further.', 'recommendation'),
        ]:
            run_ec('add', '--json', json.dumps({
                'section_id': sec, 'text': text, 'claim_type': ctype,
            }), '--dir', self.tmpdir)

    def tearDown(self):
        shutil.rmtree(self.tmpdir, ignore_errors=True)

    def test_list_all(self):
        out = run_ec('list', '--dir', self.tmpdir)
        self.assertEqual(out['count'], 4)

    def test_list_by_section(self):
        out = run_ec('list', '--dir', self.tmpdir, '--section', 'finding_1')
        self.assertEqual(out['count'], 2)

    def test_list_by_type(self):
        out = run_ec('list', '--dir', self.tmpdir, '--type', 'recommendation')
        self.assertEqual(out['count'], 1)

    def test_stats(self):
        out = run_ec('stats', '--dir', self.tmpdir)
        self.assertEqual(out['total'], 4)
        self.assertEqual(out['by_type']['factual'], 2)
        self.assertEqual(out['by_type']['synthesis'], 1)
        self.assertEqual(out['by_type']['recommendation'], 1)


class TestClaimID(unittest.TestCase):
    """Unit tests for compute_claim_id."""

    @classmethod
    def setUpClass(cls):
        sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'scripts'))
        from extract_claims import compute_claim_id, classify_claim
        cls.compute_id = staticmethod(compute_claim_id)
        cls.classify = staticmethod(classify_claim)

    def test_deterministic(self):
        id1 = self.compute_id('finding_1', 'Test claim.')
        id2 = self.compute_id('finding_1', 'Test claim.')
        self.assertEqual(id1, id2)

    def test_section_matters(self):
        id1 = self.compute_id('finding_1', 'Same text.')
        id2 = self.compute_id('finding_2', 'Same text.')
        self.assertNotEqual(id1, id2)

    def test_classify_recommendation(self):
        self.assertEqual(
            self.classify('Organizations should adopt PQC immediately.', 'recommendations'),
            'recommendation',
        )

    def test_classify_factual(self):
        self.assertEqual(
            self.classify('RSA-2048 uses 2048-bit keys.', 'finding_1'),
            'factual',
        )

    def test_classify_synthesis(self):
        self.assertEqual(
            self.classify('Taken together, the results indicate a clear trend.', 'synthesis'),
            'synthesis',
        )


if __name__ == '__main__':
    unittest.main()
