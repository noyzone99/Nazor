import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


spec = importlib.util.spec_from_file_location('gallery_fidelity', Path(__file__).resolve().parents[2] / 'Scripts/Tests/verify-gallery-fidelity.py')
fidelity = importlib.util.module_from_spec(spec)
spec.loader.exec_module(fidelity)


class GalleryFidelityTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.metadata = dict(scenario='dialog-half', platformBuild='test-os', dpiScale=1, clientSize=[2, 1], units='logical-pixels', fontHashes={'font': '0' * 64}, culture='en-US', theme='NazorLeaf', colorScheme='light', accent='blue', density='compact', contrast='normal', input='keyboard', motion='normal', timeMilliseconds=275, renderer='test', sourceRevision='fixture', referenceVersion='fixture', clockResolutionMilliseconds=1)

    def capture(self, name, pixels, **changes):
        fidelity.png_write(self.root / (name + '.png'), 2, 1, pixels)
        file = self.root / (name + '.json')
        file.write_text(json.dumps({**self.metadata, 'image': name + '.png', **changes}), encoding='utf-8')
        return file

    def test_lossless_rgba_and_transparent_pixels_round_trip(self):
        pixels = b'\x00\x01\x02\x00\x03\x04\x05\xff'
        file = self.capture('roundtrip', pixels)
        self.assertEqual((2, 1, pixels), fidelity.png_read(file.with_suffix('.png')))

    def test_browser_rgb_captures_decode_all_five_standard_scanline_filters(self):
        rows = [
            [10, 20, 30, 40, 50, 60, 11, 22, 33, 44, 55, 66],
            [10, 20, 30, 30, 30, 30, 11, 22, 33, 33, 33, 33],
            [10, 20, 30, 40, 50, 60, 1, 2, 3, 4, 5, 6],
            [10, 20, 30, 35, 40, 45, 6, 12, 18, 19, 19, 20],
            [10, 20, 30, 30, 30, 30, 1, 2, 3, 4, 5, 6],
        ]
        def chunk(kind, data):
            return fidelity.struct.pack('>I', len(data)) + kind + data + fidelity.struct.pack('>I', fidelity.zlib.crc32(kind + data) & 0xffffffff)
        expected = bytes([10, 20, 30, 255, 40, 50, 60, 255, 11, 22, 33, 255, 44, 55, 66, 255])
        for filtering, row in enumerate(rows):
            with self.subTest(filter=filtering):
                raw = bytes([filtering] + row[:6] + [filtering] + row[6:])
                file = self.root / 'filtered.png'
                file.write_bytes(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', fidelity.struct.pack('>IIBBBBB', 2, 2, 8, 2, 0, 0, 0)) + chunk(b'IDAT', fidelity.zlib.compress(raw)) + chunk(b'IEND', b''))
                self.assertEqual((2, 2, expected), fidelity.png_read(file))

    def test_identical_complete_captures_pass_without_tolerance(self):
        pixels = bytes(8)
        result = fidelity.compare(self.capture('reference', pixels), self.capture('actual', pixels), self.root)
        self.assertEqual('pass', result['status'])
        self.assertEqual(0, result['differentPixels'])
        self.assertEqual(0, result['tolerance'])
        self.assertEqual([], result['masks'])

    def test_one_changed_channel_is_a_failure_with_exact_pixel_bounds(self):
        result = fidelity.compare(self.capture('reference', bytes(8)), self.capture('actual', bytes(7) + b'\x01'), self.root)
        self.assertEqual('different', result['status'])
        self.assertEqual(1, result['differentPixels'])
        self.assertEqual([1, 0, 1, 0], result['bounds'])
        self.assertEqual(1, result['maximumChannelDelta'])

    def test_identically_wrong_dimensions_and_unknown_units_cannot_pass(self):
        for changes in ({'clientSize': [800, 600], 'dpiScale': 1.5}, {'units': 'unknown'}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                fidelity.compare(self.capture('reference', bytes(8), **changes), self.capture('actual', bytes(8), **changes), self.root)

    def test_logical_extent_uses_ceiling_at_fractional_dpi_and_physical_units_do_not_scale(self):
        for units, size in (('logical-pixels', [2, 1]), ('physical-pixels', [3, 2])):
            files = [self.capture(name, bytes(8), dpiScale=1.5, clientSize=size, units=units) for name in ('reference', 'actual')]
            for file in files:
                fidelity.png_write(file.with_suffix('.png'), 3, 2, bytes(24))
            self.assertEqual('pass', fidelity.compare(*files, self.root)['status'])

    def test_empty_png_dimensions_are_rejected(self):
        for width, height in ((0, 1), (1, 0)):
            file = self.root / 'empty.png'
            fidelity.png_write(file, width, height, b'')
            with self.subTest(width=width, height=height), self.assertRaises(ValueError):
                fidelity.png_read(file)

    def test_empty_or_wrong_typed_identity_and_boolean_metrics_are_rejected(self):
        for changes in ({'renderer': ''}, {'sourceRevision': ' '}, {'scenario': ''}, {'theme': {}}, {'dpiScale': True}, {'clockResolutionMilliseconds': True}, {'timeMilliseconds': False}, {'clientSize': [True, 1]}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                fidelity.compare(self.capture('reference', bytes(8), **changes), self.capture('actual', bytes(8), **changes), self.root)

    def test_unknown_dpi_and_mismatched_motion_refuse_comparison(self):
        reference = self.capture('reference', bytes(8))
        for changes in ({'dpiScale': None}, {'motion': 'reduced'}, {'colorScheme': 'dark'}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                fidelity.compare(reference, self.capture('actual', bytes(8), **changes), self.root)

    def test_unknown_fonts_and_invalid_capture_metrics_refuse_comparison(self):
        for changes in ({'fontHashes': {}}, {'fontHashes': {'font': 'unknown'}}, {'clockResolutionMilliseconds': 0}, {'clientSize': [0, 100]}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                fidelity.compare(self.capture('reference', bytes(8), **changes), self.capture('actual', bytes(8), **changes), self.root)

    def test_corrupted_png_does_not_become_a_visual_pass(self):
        file = self.capture('reference', bytes(8)).with_suffix('.png')
        raw = bytearray(file.read_bytes())
        raw[29] ^= 1
        file.write_bytes(raw)
        with self.assertRaises(ValueError):
            fidelity.png_read(file)

    def test_source_lifecycle_boundaries_cannot_be_certified_even_by_self_comparison(self):
        file = self.capture('unstable', bytes(8), capture={'candidateStatus': 'source-lifecycle-boundary-review-required'})
        with self.assertRaisesRegex(ValueError, 'lifecycle'):
            fidelity.compare(file, file, self.root)

    def test_repeated_targeted_results_do_not_inflate_full_suite_coverage(self):
        manifest = self.root / 'manifest.json'
        manifest.write_text(json.dumps({'schemaVersion': 1, 'inputs': [], 'reference': {}, 'commonLimitations': [], 'families': [{'id': 'test', 'checks': ['ExampleTests']}]}), encoding='utf-8')
        results = []
        for name, result in (('full', 'Pass'), ('rerun', 'Fail')):
            file = self.root / (name + '.xml')
            file.write_text('<assemblies><assembly name="Tests"><collection><test name="Example" type="Tests.ExampleTests" result="' + result + '" /></collection></assembly></assemblies>', encoding='utf-8')
            results.append(file)
        with patch.object(fidelity, 'fingerprint', return_value={}):
            result = fidelity.report(manifest, results)
        self.assertEqual({'Fail': 1}, result['families'][0]['results']['ExampleTests'])
        self.assertEqual('fail', result['families'][0]['automatedStatus'])
        self.assertEqual('pending', result['nativeAcceptance'])

    def test_runner_errors_and_unsupported_case_outcomes_never_certify_families(self):
        manifest = self.root / 'manifest.json'
        manifest.write_text(json.dumps({'schemaVersion': 1, 'inputs': [], 'reference': {}, 'commonLimitations': [], 'families': [{'id': 'test', 'checks': ['ExampleTests']}]}), encoding='utf-8')
        for errors, outcome, details in ((1, 'Pass', ''), (0, 'Pass', '<errors><error type="fixture">cleanup failed</error></errors>'), (0, 'NotRun', ''), (0, 'unexpected', '')):
            file = self.root / 'errors.xml'
            file.write_text(f'<assemblies><assembly name="Tests" errors="{errors}">{details}<collection><test name="Example" type="Tests.ExampleTests" result="{outcome}" /></collection></assembly></assemblies>', encoding='utf-8')
            with self.subTest(errors=errors, outcome=outcome, details=details), patch.object(fidelity, 'fingerprint', return_value={}):
                result = fidelity.report(manifest, [file])
                self.assertEqual('fail', result['families'][0]['automatedStatus'])
                self.assertEqual(bool(errors or details), bool(result['runnerErrors']))

    def test_fully_qualified_checks_do_not_borrow_results_from_same_named_classes(self):
        manifest = self.root / 'manifest.json'
        manifest.write_text(json.dumps({'schemaVersion': 1, 'inputs': [], 'reference': {}, 'commonLimitations': [], 'families': [{'id': 'test', 'checks': ['Style.EscapeTests', 'Editor.EscapeTests']}]}), encoding='utf-8')
        file = self.root / 'style.xml'
        file.write_text('<assemblies><assembly name="Style"><collection><test name="Example" type="Style.EscapeTests" result="Pass" /></collection></assembly></assemblies>', encoding='utf-8')
        with patch.object(fidelity, 'fingerprint', return_value={}):
            result = fidelity.report(manifest, [file])
        self.assertEqual('not-run', result['families'][0]['automatedStatus'])
        self.assertEqual(['Editor.EscapeTests'], result['families'][0]['missingChecks'])
        self.assertEqual({'Pass': 1}, result['families'][0]['results']['Style.EscapeTests'])


if __name__ == '__main__':
    unittest.main()
